using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Utils;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Fig.WebHooks.Contracts;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class WebHookDisseminationService : IWebHookDisseminationService
{
    private readonly IWebHookRepository _webHookRepository;
    private readonly IWebHookClientRepository _webHookClientRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly ILogger<WebHookDisseminationService> _logger;
    private readonly HttpClient _httpClient;

    public WebHookDisseminationService(IHttpClientFactory httpClientFactory,
        IWebHookRepository webHookRepository,
        IWebHookClientRepository webHookClientRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        ILogger<WebHookDisseminationService> logger)
    {
        _webHookRepository = webHookRepository;
        _webHookClientRepository = webHookClientRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _logger = logger;

        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(2);
    }

    public async Task NewClientRegistration(SettingClientBusinessEntity client)
    {
        const WebHookType type = WebHookType.NewClientRegistration;
        await SendWebHook(type, 
            () => GetMatchingWebHooks(type, client),
            _ => new ClientRegistrationDataContract(client.Name, client.Instance,
            client.Settings.Select(a => a.Name).ToList()));
    }

    public async Task UpdatedClientRegistration(SettingClientBusinessEntity client)
    {
        const WebHookType type = WebHookType.UpdatedClientRegistration;
        await SendWebHook(type,
            () => GetMatchingWebHooks(type, client),
            _ => new ClientRegistrationDataContract(client.Name, client.Instance,
                client.Settings.Select(a => a.Name).ToList()));
    }

    public async Task SettingValueChanged(List<ChangedSetting> changes, SettingClientBusinessEntity client, string? instance,
        string? username)
    {
        const WebHookType type = WebHookType.SettingValueChanged;
        await SendWebHook(type,
            () => GetMatchingWebHooks(type, client),
            webHook => new SettingValueChangedDataContract(client.Name, client.Instance,
                changes.Where(webHook.IsMatch).Select(a => a.Name).ToList(), username));
    }

    public async Task MemoryLeakDetected(ClientStatusBusinessEntity client, ClientRunSessionBusinessEntity session)
    {
        if (session.MemoryAnalysis is null)
            return;
        
        const WebHookType type = WebHookType.SettingValueChanged;
        await SendWebHook(type, 
            () => GetMatchingWebHooks(type, client),
            _ => new MemoryLeakDetectedDataContract(client.Name, client.Instance,
                session.MemoryAnalysis.TrendLineSlope, session.MemoryAnalysis.StartingBytesAverage, 
                session.MemoryAnalysis.EndingBytesAverage, session.MemoryAnalysis.SecondsAnalyzed, 
                session.MemoryAnalysis.DataPointsAnalyzed));
    }

    public async Task ClientConnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client)
    {
        const WebHookType type = WebHookType.ClientStatusChanged;
        await SendWebHook(type, 
            () => GetMatchingWebHooks(type, client),
            _ => new ClientStatusChangedDataContract(client.Name, client.Instance,
                ConnectionEvent.Connected, session.UptimeSeconds, session.IpAddress,
                session.Hostname, session.FigVersion, session.ApplicationVersion));

        await SendMinRunSessionsWebHook(client, ConnectionEvent.Connected);
    }

    public async Task ClientDisconnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client)
    {
        const WebHookType type = WebHookType.ClientStatusChanged;
        await SendWebHook(type, 
            () => GetMatchingWebHooks(type, client),
            _ => new ClientStatusChangedDataContract(client.Name, client.Instance,
                ConnectionEvent.Disconnected, session.UptimeSeconds, session.IpAddress,
                session.Hostname, session.FigVersion, session.ApplicationVersion));
        
        await SendMinRunSessionsWebHook(client, ConnectionEvent.Connected);
    }
    
    private async Task SendMinRunSessionsWebHook(ClientStatusBusinessEntity client, ConnectionEvent connectionEvent)
    {
        var webHooks = GetMatchingWebHooks(WebHookType.MinRunSessions, client);

        var webHookClients = GetWebHookClients(webHooks);
        
        foreach (var webHook in webHooks)
        {
            var webHookClient = webHookClients.First(a => a.Id == webHook.ClientId);
            if (connectionEvent == ConnectionEvent.Connected && client.RunSessions.Count == webHook.MinSessions)
            {
                await Send(webHookClient, RunSessionsEvent.MinimumRestored);
            }
            else if (connectionEvent == ConnectionEvent.Disconnected &&
                     client.RunSessions.Count == webHook.MinSessions - 1)
            {
                await Send(webHookClient, RunSessionsEvent.BelowMinimum);
            }
        }

        async Task Send(WebHookClientBusinessEntity webHookClient, RunSessionsEvent runSessionsEvent)
        {
            var contract = new MinRunSessionsDataContract(client.Name, client.Instance, client.RunSessions.Count,
                runSessionsEvent);
            var request = CreateRequest(webHookClient, WebHookType.MinRunSessions, contract);

            var result = await SendRequest(request, webHookClient.Name);
            LogWebHookSendingEvent(WebHookType.MinRunSessions, webHookClient, result.Message);
        }
    }
    
    private async Task SendWebHook(WebHookType webHookType, Func<List<WebHookBusinessEntity>> getMatchingWebHooks, Func<WebHookBusinessEntity, object> createContract)
    {
        var webHooks = getMatchingWebHooks();
        
        if (!webHooks.Any())
            return;

        var webHookClients = GetWebHookClients(webHooks);
        
        foreach (var webHook in webHooks)
        {
            var webHookClient = webHookClients.First(a => a.Id == webHook.ClientId);
            var contract = createContract(webHook);
            var request = CreateRequest(webHookClient, webHookType, contract);

            var result = await SendRequest(request, webHookClient.Name);
            LogWebHookSendingEvent(webHook.WebHookType, webHookClient, result.Message);
        }
    }

    private void LogWebHookSendingEvent(WebHookType webHookType, WebHookClientBusinessEntity webHookClient, string result)
    {
        _eventLogRepository.Add(_eventLogFactory.WebHookSent(webHookType, webHookClient, result));
    }

    private HttpRequestMessage CreateRequest(WebHookClientBusinessEntity client, WebHookType webHookType, object value)
    {
        var route = webHookType.GetRoute();
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(client.BaseUri), route));
        request.Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Secret", client.Secret);

        return request;
    }

    private async Task<RequestResult> SendRequest(HttpRequestMessage request, string clientName)
    {
        try
        {
            var result = await _httpClient.SendAsync(request);
            if (!result.IsSuccessStatusCode)
                _logger.LogWarning("Failed to send webhook to client named {WebHookClientName} at address {RequestUri}. Status Code: {StatusCode}",
                    clientName,
                    request.RequestUri,
                    result.StatusCode);
            return new RequestResult(result.IsSuccessStatusCode, result.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to contact web hook client named {WebHookClientName} at address {RequestUri}",
                clientName,
                request.RequestUri);
            return new RequestResult(false);
        }
    }

    private List<WebHookBusinessEntity> GetMatchingWebHooks(
        WebHookType webHookType,
        SettingClientBusinessEntity client)
    {
        var webHooks = _webHookRepository.GetWebHooksByType(webHookType);
        return webHooks.Where(a => a.IsMatch(client)).ToList();
    }
    
    private List<WebHookBusinessEntity> GetMatchingWebHooks(
        WebHookType webHookType,
        ClientStatusBusinessEntity client)
    {
        var webHooks = _webHookRepository.GetWebHooksByType(webHookType);
        return webHooks.Where(a => a.IsMatch(client)).ToList();
    }

    private List<WebHookClientBusinessEntity> GetWebHookClients(List<WebHookBusinessEntity> webHooks)
    {
        var clientIds = webHooks.Select(a => a.ClientId).Distinct();
        return _webHookClientRepository.GetClients(clientIds).ToList();
    }

    private class RequestResult
    {
        public RequestResult(bool wasSuccessful, HttpStatusCode? statusCode = null)
        {
            WasSuccessful = wasSuccessful;
            StatusCode = statusCode;
        }

        public bool WasSuccessful { get; }
        
        public HttpStatusCode? StatusCode { get; }

        public string Message
        {
            get
            {
                if (WasSuccessful)
                    return "Succeeded";

                if (StatusCode == null)
                    return "Failed (error)";

                return $"Failed {StatusCode}";
            }
        }
    }
}