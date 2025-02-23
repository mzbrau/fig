using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Utils;
using Fig.Contracts.Status;
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
    private readonly IConfigurationRepository _configurationRepository;
    private readonly HttpClient _httpClient;

    public WebHookDisseminationService(IHttpClientFactory httpClientFactory,
        IWebHookRepository webHookRepository,
        IWebHookClientRepository webHookClientRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        ILogger<WebHookDisseminationService> logger,
        IConfigurationRepository configurationRepository)
    {
        _webHookRepository = webHookRepository;
        _webHookClientRepository = webHookClientRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _logger = logger;
        _configurationRepository = configurationRepository;

        _httpClient = httpClientFactory.CreateClient();
        if (!_httpClient.Timeout.Equals(TimeSpan.FromSeconds(2)))
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
    }

    public async Task NewClientRegistration(SettingClientBusinessEntity client)
    {
        const WebHookType type = WebHookType.NewClientRegistration;
        var uri = await GetUri(type);
        await SendWebHook(type, 
            () => GetMatchingWebHooks(type, client),
            _ => new ClientRegistrationDataContract(client.Name, client.Instance,
            client.Settings.Select(a => a.Name).ToList(), RegistrationType.New, uri), _ => true);
    }

    public async Task UpdatedClientRegistration(SettingClientBusinessEntity client)
    {
        const WebHookType type = WebHookType.UpdatedClientRegistration;
        var uri = await GetUri(type);
        await SendWebHook(type,
            () => GetMatchingWebHooks(type, client),
            _ => new ClientRegistrationDataContract(client.Name, client.Instance,
                client.Settings.Select(a => a.Name).ToList(), RegistrationType.Updated, uri), _ => true);
    }

    public async Task SettingValueChanged(List<ChangedSetting> changes, SettingClientBusinessEntity client,
        string? username, string changeMessage)
    {
        const WebHookType type = WebHookType.SettingValueChanged;
        var uri = await GetUri(type);
        await SendWebHook(type,
            () => GetMatchingWebHooks(type, client),
            webHook => new SettingValueChangedDataContract(client.Name, client.Instance,
                changes.Where(webHook.IsMatch).Select(a => a.Name).ToList(), username, changeMessage,uri),
            c =>
            {
                var contract = (SettingValueChangedDataContract)c;
                return contract.UpdatedSettings.Any();
            });
    }

    public async Task ClientConnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client)
    {
        const WebHookType type = WebHookType.ClientStatusChanged;
        var uri = await GetUri(type);
        await SendWebHook(type, 
            () => GetMatchingWebHooks(type, client),
            _ => new ClientStatusChangedDataContract(client.Name, client.Instance,
                ConnectionEvent.Connected, session.StartTimeUtc, session.IpAddress,
                session.Hostname, session.FigVersion, session.ApplicationVersion, uri), _ => true);

        await SendMinRunSessionsWebHook(client, ConnectionEvent.Connected);
    }

    public async Task ClientDisconnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client)
    {
        const WebHookType type = WebHookType.ClientStatusChanged;
        var uri = await GetUri(type);
        await SendWebHook(type, 
            () => GetMatchingWebHooks(type, client),
            _ => new ClientStatusChangedDataContract(client.Name, client.Instance,
                ConnectionEvent.Disconnected, session.StartTimeUtc, session.IpAddress,
                session.Hostname, session.FigVersion, session.ApplicationVersion, uri), _ => true);
        
        await SendMinRunSessionsWebHook(client, ConnectionEvent.Disconnected);
    }

    public async Task ConfigurationErrorStatusChanged(ClientStatusBusinessEntity client, StatusRequestDataContract statusRequest)
    {
        const WebHookType type = WebHookType.ConfigurationError;
        var uri = await GetUri(type);
        var status = statusRequest.HasConfigurationError
            ? ConfigurationErrorStatus.Error
            : ConfigurationErrorStatus.Resolved;
        await SendWebHook(type, 
            () => GetMatchingWebHooks(type, client),
            _ => new ClientConfigurationErrorDataContract(client.Name, client.Instance,
                status, statusRequest.FigVersion, statusRequest.ApplicationVersion, 
                statusRequest.ConfigurationErrors, uri), _ => true);
    }

    private async Task SendMinRunSessionsWebHook(ClientStatusBusinessEntity client, ConnectionEvent connectionEvent)
    {
        var webHooks = await GetMatchingWebHooks(WebHookType.MinRunSessions, client);

        var webHookClients = await GetWebHookClients(webHooks);
        
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
                runSessionsEvent, await GetUri(WebHookType.MinRunSessions));
            var request = CreateRequest(webHookClient, WebHookType.MinRunSessions, contract);

            var result = await SendRequest(request, webHookClient.Name);
            await LogWebHookSendingEvent(WebHookType.MinRunSessions, webHookClient, result.Message);
        }
    }
    
    private async Task SendWebHook(WebHookType webHookType,
        Func<Task<List<WebHookBusinessEntity>>> getMatchingWebHooks,
        Func<WebHookBusinessEntity, object> createContract,
        Func<object, bool> shouldSend)
    {
        var webHooks = await getMatchingWebHooks();
        
        if (!webHooks.Any())
            return;

        var webHookClients = await GetWebHookClients(webHooks);
        
        foreach (var webHook in webHooks)
        {
            var webHookClient = webHookClients.First(a => a.Id == webHook.ClientId);
            var contract = createContract(webHook);

            if (!shouldSend(contract))
                continue;
            
            var request = CreateRequest(webHookClient, webHookType, contract);

            var result = await SendRequest(request, webHookClient.Name);
            await LogWebHookSendingEvent(webHook.WebHookType, webHookClient, result.Message);
        }
    }

    private async Task LogWebHookSendingEvent(WebHookType webHookType, WebHookClientBusinessEntity webHookClient, string result)
    {
        await _eventLogRepository.Add(_eventLogFactory.WebHookSent(webHookType, webHookClient, result));
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
                _logger.LogWarning(
                    "Failed to send webhook to client named {WebHookClientName} at address {RequestUri}. Status Code: {StatusCode}",
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
            return new RequestResult(false, ex.Message);
        }
    }

    private async Task<Uri?> GetUri(WebHookType webHookType)
    {
        var baseAddress = (await _configurationRepository.GetConfiguration()).WebApplicationBaseAddress ?? "https://localhost:7148/";

        var route = webHookType switch
        {
            WebHookType.ClientStatusChanged => "clients",
            WebHookType.SettingValueChanged => string.Empty,
            WebHookType.NewClientRegistration => string.Empty,
            WebHookType.UpdatedClientRegistration => string.Empty,
            WebHookType.MinRunSessions => "clients",
            WebHookType.ConfigurationError => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(webHookType), webHookType, null)
        };

        return new Uri(new Uri(baseAddress), route);
    }

    private async Task<List<WebHookBusinessEntity>> GetMatchingWebHooks(
        WebHookType webHookType,
        SettingClientBusinessEntity client)
    {
        var webHooks = await _webHookRepository.GetWebHooksByType(webHookType);
        return webHooks.Where(a => a.IsMatch(client)).ToList();
    }
    
    private async Task<List<WebHookBusinessEntity>> GetMatchingWebHooks(
        WebHookType webHookType,
        ClientStatusBusinessEntity client)
    {
        var webHooks = await _webHookRepository.GetWebHooksByType(webHookType);
        return webHooks.Where(a => a.IsMatch(client)).ToList();
    }

    private async Task<List<WebHookClientBusinessEntity>> GetWebHookClients(List<WebHookBusinessEntity> webHooks)
    {
        var clientIds = webHooks.Select(a => a.ClientId).Distinct();
        return (await _webHookClientRepository.GetClients(clientIds)).ToList();
    }

    private class RequestResult
    {
        private readonly bool _wasSuccessful;
        private readonly HttpStatusCode? _statusCode;
        private readonly string? _message;

        public RequestResult(bool wasSuccessful, HttpStatusCode? statusCode)
        {
            _wasSuccessful = wasSuccessful;
            _statusCode = statusCode;
        }

        public RequestResult(bool wasSuccessful, string message)
        {
            _wasSuccessful = wasSuccessful;
            _message = message;
        }

        public string Message
        {
            get
            {
                if (_wasSuccessful)
                    return "Succeeded";

                if (_message is not null)
                    return $"Failed ({_message})";
                
                if (_statusCode is not null)
                    return $"Failed ({_statusCode})";

                return "Failed (Unknown error)";
            }
        }
    }
}