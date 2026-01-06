using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.WebHooks;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Fig.WebHooks.Contracts;
using Newtonsoft.Json;

namespace Fig.Api.Workers;

public class WebHookProcessorWorker : BackgroundService
{
    private readonly IWebHookQueue _webHookQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WebHookProcessorWorker> _logger;
    private readonly HttpClient _httpClient;

    public WebHookProcessorWorker(
        IWebHookQueue webHookQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<WebHookProcessorWorker> logger,
        IHttpClientFactory httpClientFactory)
    {
        _webHookQueue = webHookQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        // Increase timeout to 10 seconds to prevent premature cancellation during integration tests
        if (!_httpClient.Timeout.Equals(TimeSpan.FromSeconds(10)))
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebHook processor worker started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var webHookItem = _webHookQueue.DequeueWebHook();
                
                if (webHookItem != null)
                {
                    await ProcessWebHook(webHookItem, stoppingToken);
                }
                else
                {
                    // No webhooks to process, wait a bit before checking again
                    try
                    {
                        await Task.Delay(100, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected during shutdown, exit gracefully
                        _logger.LogInformation("WebHook processor worker stopping (idle delay cancelled)");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown, exit gracefully
                _logger.LogInformation("WebHook processor worker stopping (operation cancelled)");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook queue");
                // Wait a bit longer after an error to avoid tight error loops
                try
                {
                    await Task.Delay(1000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown, exit gracefully
                    _logger.LogInformation("WebHook processor worker stopping (error delay cancelled)");
                    break;
                }
            }
        }
        
        _logger.LogInformation("WebHook processor worker stopped");
    }

    private async Task ProcessWebHook(WebHookQueueItem item, CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        
        try
        {
            var webHookClientRepository = scope.ServiceProvider.GetRequiredService<IWebHookClientRepository>();
            var eventLogRepository = scope.ServiceProvider.GetRequiredService<IEventLogRepository>();
            var eventLogFactory = scope.ServiceProvider.GetRequiredService<IEventLogFactory>();
            var configurationRepository = scope.ServiceProvider.GetRequiredService<IConfigurationRepository>();
            var webHookHealthConverter = scope.ServiceProvider.GetRequiredService<IWebHookHealthConverter>();

            if (!item.MatchingWebHooks.Any())
                return;

            var webHookClients = await GetWebHookClients(webHookClientRepository, item.MatchingWebHooks);
            
            foreach (var webHook in item.MatchingWebHooks)
            {
                // Check for cancellation before processing each webhook
                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("WebHook processing cancelled before sending to client {ClientId}", webHook.ClientId);
                    break;
                }
                
                var webHookClient = webHookClients.First(a => a.Id == webHook.ClientId);
                var contract = await CreateContract(item.WebHookType, item.WebHookData, webHook, configurationRepository, webHookHealthConverter);

                if (!ShouldSend(contract))
                    continue;
                
                var request = CreateRequest(webHookClient, item.WebHookType, contract);
                var result = await SendRequest(request, webHookClient.Name, stoppingToken);
                await LogWebHookSendingEvent(item.WebHookType, webHookClient, result.Message, eventLogRepository, eventLogFactory);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebHook processing cancelled for type {WebHookType}", item.WebHookType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook of type {WebHookType}", item.WebHookType);
        }
    }

    private async Task<List<WebHookClientBusinessEntity>> GetWebHookClients(IWebHookClientRepository repository, List<WebHookBusinessEntity> webHooks)
    {
        var clientIds = webHooks.Select(a => a.ClientId).Distinct();
        return (await repository.GetClients(clientIds)).ToList();
    }

    private async Task<object> CreateContract(WebHookType webHookType, object webHookData, WebHookBusinessEntity webHook, IConfigurationRepository configurationRepository, IWebHookHealthConverter webHookHealthConverter)
    {
        var uri = await GetUri(webHookType, configurationRepository);
        
        return webHookType switch
        {
            WebHookType.NewClientRegistration => CreateNewClientRegistrationContract((NewClientRegistrationWebHookData)webHookData, uri),
            WebHookType.UpdatedClientRegistration => CreateUpdatedClientRegistrationContract((UpdatedClientRegistrationWebHookData)webHookData, uri),
            WebHookType.SettingValueChanged => CreateSettingValueChangedContract((SettingValueChangedWebHookData)webHookData, webHook, uri),
            WebHookType.ClientStatusChanged when webHookData is ClientConnectedWebHookData data => CreateClientConnectedContract(data, uri),
            WebHookType.ClientStatusChanged when webHookData is ClientDisconnectedWebHookData data => CreateClientDisconnectedContract(data, uri),
            WebHookType.HealthStatusChanged => CreateHealthStatusChangedContract((HealthStatusChangedWebHookData)webHookData, webHookHealthConverter, uri),
            WebHookType.MinRunSessions => CreateMinRunSessionsContract((MinRunSessionsWebHookData)webHookData, uri),
            WebHookType.SecurityEvent => CreateSecurityEventContract((SecurityEventWebHookData)webHookData, uri),
            _ => throw new ArgumentOutOfRangeException(nameof(webHookType), webHookType, null)
        };
    }

    private object CreateNewClientRegistrationContract(NewClientRegistrationWebHookData data, Uri? uri)
    {
        return new ClientRegistrationDataContract(data.Client.Name, data.Client.Instance,
            data.Client.Settings.Select(a => a.Name).ToList(), RegistrationType.New, uri);
    }

    private object CreateUpdatedClientRegistrationContract(UpdatedClientRegistrationWebHookData data, Uri? uri)
    {
        return new ClientRegistrationDataContract(data.Client.Name, data.Client.Instance,
            data.Client.Settings.Select(a => a.Name).ToList(), RegistrationType.Updated, uri);
    }

    private object CreateSettingValueChangedContract(SettingValueChangedWebHookData data, WebHookBusinessEntity webHook, Uri? uri)
    {
        return new SettingValueChangedDataContract(data.Client.Name, data.Client.Instance,
            data.Changes.Where(webHook.IsMatch).Select(a => a.Name).ToList(), data.Username, data.ChangeMessage, uri);
    }

    private object CreateClientConnectedContract(ClientConnectedWebHookData data, Uri? uri)
    {
        return new ClientStatusChangedDataContract(data.Client.Name, data.Client.Instance,
            ConnectionEvent.Connected, data.Session.StartTimeUtc, data.Session.IpAddress,
            data.Session.Hostname, data.Session.FigVersion, data.Session.ApplicationVersion, uri);
    }

    private object CreateClientDisconnectedContract(ClientDisconnectedWebHookData data, Uri? uri)
    {
        return new ClientStatusChangedDataContract(data.Client.Name, data.Client.Instance,
            ConnectionEvent.Disconnected, data.Session.StartTimeUtc, data.Session.IpAddress,
            data.Session.Hostname, data.Session.FigVersion, data.Session.ApplicationVersion, uri);
    }

    private object CreateHealthStatusChangedContract(HealthStatusChangedWebHookData data, IWebHookHealthConverter converter, Uri? uri)
    {
        var status = converter.Convert(data.HealthDetails.Status);
        var health = converter.Convert(data.HealthDetails);
        return new ClientHealthChangedDataContract(data.Client.Name, data.Client.Instance, data.Session.Hostname, data.Session.IpAddress,
            status, data.Session.FigVersion, data.Session.ApplicationVersion, health, uri);
    }

    private object CreateMinRunSessionsContract(MinRunSessionsWebHookData data, Uri? uri)
    {
        // Use the override session count if provided, otherwise use the client's current session count
        var sessionCount = data.SessionCount ?? data.Client.RunSessions.Count;
        return new MinRunSessionsDataContract(data.Client.Name, data.Client.Instance, sessionCount,
            data.RunSessionsEvent, uri);
    }

    private object CreateSecurityEventContract(SecurityEventWebHookData data, Uri? uri)
    {
        return new SecurityEventDataContract(
            data.EventType, 
            data.Timestamp, 
            data.Username, 
            data.Success,
            data.IpAddress, 
            data.Hostname, 
            data.FailureReason, 
            uri);
    }

    private bool ShouldSend(object contract)
    {
        return contract switch
        {
            SettingValueChangedDataContract settingContract => settingContract.UpdatedSettings.Any(),
            _ => true
        };
    }

    private HttpRequestMessage CreateRequest(WebHookClientBusinessEntity client, WebHookType webHookType, object value)
    {
        var route = webHookType.GetRoute();
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(client.BaseUri), route));
        request.Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Secret", client.Secret);

        return request;
    }

    private async Task<RequestResult> SendRequest(HttpRequestMessage request, string clientName, CancellationToken stoppingToken)
    {
        try
        {
            var result = await _httpClient.SendAsync(request, stoppingToken);
            if (!result.IsSuccessStatusCode)
                _logger.LogWarning(
                    "Failed to send webhook to client named {WebHookClientName} at address {RequestUri}. Status Code: {StatusCode}",
                    clientName,
                    request.RequestUri,
                    result.StatusCode);
            return new RequestResult(result.IsSuccessStatusCode, result.StatusCode);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebHook request to {WebHookClientName} at {RequestUri} was cancelled",
                clientName,
                request.RequestUri);
            return new RequestResult(false, "Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to contact web hook client named {WebHookClientName} at address {RequestUri}",
                clientName,
                request.RequestUri);
            return new RequestResult(false, ex.Message);
        }
    }

    private async Task<Uri?> GetUri(WebHookType webHookType, IConfigurationRepository configurationRepository)
    {
        var baseAddress = (await configurationRepository.GetConfiguration()).WebApplicationBaseAddress ?? "https://localhost:7148/";

        var route = webHookType switch
        {
            WebHookType.ClientStatusChanged => "clients",
            WebHookType.SettingValueChanged => string.Empty,
            WebHookType.NewClientRegistration => string.Empty,
            WebHookType.UpdatedClientRegistration => string.Empty,
            WebHookType.MinRunSessions => "clients",
            WebHookType.HealthStatusChanged => "clients",
            WebHookType.SecurityEvent => "security",
            _ => throw new ArgumentOutOfRangeException(nameof(webHookType), webHookType, null)
        };

        return new Uri(new Uri(baseAddress), route);
    }

    private async Task LogWebHookSendingEvent(WebHookType webHookType, WebHookClientBusinessEntity webHookClient, string result, IEventLogRepository eventLogRepository, IEventLogFactory eventLogFactory)
    {
        await eventLogRepository.Add(eventLogFactory.WebHookSent(webHookType, webHookClient, result));
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

    public override void Dispose()
    {
        _httpClient.Dispose();
        base.Dispose();
    }
}
