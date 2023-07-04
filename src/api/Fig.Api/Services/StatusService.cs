using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.ExtensionMethods;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public class StatusService : IStatusService
{
    private readonly IClientStatusConverter _clientStatusConverter;
    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMemoryLeakAnalyzer _memoryLeakAnalyzer;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<StatusService> _logger;
    private readonly IWebHookDisseminationService _webHookDisseminationService;
    private string? _requesterHostname;
    private string? _requestIpAddress;

    public StatusService(
        IClientStatusRepository clientStatusRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IClientStatusConverter clientStatusConverter,
        IConfigurationRepository configurationRepository,
        IMemoryLeakAnalyzer memoryLeakAnalyzer,
        ILogger<StatusService> logger,
        IWebHookDisseminationService webHookDisseminationService)
    {
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _clientStatusConverter = clientStatusConverter;
        _configurationRepository = configurationRepository;
        _memoryLeakAnalyzer = memoryLeakAnalyzer;
        _logger = logger;
        _webHookDisseminationService = webHookDisseminationService;
    }

    public async Task<StatusResponseDataContract> SyncStatus(
        string clientName,
        string? instance,
        string clientSecret,
        StatusRequestDataContract statusRequest)
    {
        var client = _clientStatusRepository.GetClient(clientName, instance);

        if (client is null && !string.IsNullOrEmpty(instance))
            client = _clientStatusRepository.GetClient(clientName);
        
        if (client is null)
            throw new KeyNotFoundException();

        var registrationStatus = RegistrationStatusValidator.GetStatus(client, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
            throw new UnauthorizedAccessException();

        await RemoveExpiredSessions(client);
        
        var session = client.RunSessions.FirstOrDefault(a => a.RunSessionId == statusRequest.RunSessionId);
        if (session is not null)
        {
            if (session.HasConfigurationError != statusRequest.HasConfigurationError)
                HandleConfigurationErrorStatusChanged(statusRequest, client);
            
            session.Update(statusRequest, _requesterHostname, _requestIpAddress);
        }
        else
        {
            session = new ClientRunSessionBusinessEntity
            {
                RunSessionId = statusRequest.RunSessionId
            };
            session.Update(statusRequest, _requesterHostname, _requestIpAddress);
            client.RunSessions.Add(session);
            _eventLogRepository.Add(_eventLogFactory.NewSession(session, client));
            if (statusRequest.HasConfigurationError)
                HandleConfigurationErrorStatusChanged(statusRequest, client);
            await _webHookDisseminationService.ClientConnected(session, client);
        }

        var memoryAnalysis = _memoryLeakAnalyzer.AnalyzeMemoryUsage(session);
        if (memoryAnalysis is not null)
        {
            session.MemoryAnalysis = memoryAnalysis;
            if (memoryAnalysis.PossibleMemoryLeakDetected)
            {
                await _webHookDisseminationService.MemoryLeakDetected(client, session);
            }
        }

        _clientStatusRepository.UpdateClientStatus(client);
        var configuration = _configurationRepository.GetConfiguration();

        var updateAvailable = client.LastSettingValueUpdate > statusRequest.LastSettingUpdate;
        var changedSettings = GetChangedSettingNames(updateAvailable,
            statusRequest.LastSettingUpdate,
            client.LastSettingValueUpdate ?? DateTime.MinValue,
            client.Name,
            client.Instance);

        return new StatusResponseDataContract
        {
            SettingUpdateAvailable = updateAvailable,
            PollIntervalMs = session.PollIntervalMs,
            LiveReload = session.LiveReload ?? true,
            AllowOfflineSettings = configuration.AllowOfflineSettings,
            RestartRequested = session.RestartRequested,
            ChangedSettings = changedSettings,
        };
    }

    private List<string>? GetChangedSettingNames(bool updateAvailable, DateTime startTime, DateTime endTime, string clientName, string? instance)
    {
        if (!updateAvailable)
            return null;

        var start = DateTime.SpecifyKind(startTime.AddSeconds(-1), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(endTime.AddSeconds(1), DateTimeKind.Utc);
        var valueChangeLogs = _eventLogRepository.GetSettingChanges(start, end, clientName, instance);
        return valueChangeLogs
            .Where(a => a.SettingName is not null)
            .Select(a => a.SettingName!)
            .Distinct()
            .ToList();
    }

    public ClientConfigurationDataContract UpdateConfiguration(string clientName, string? instance,
        ClientConfigurationDataContract updatedConfiguration)
    {
        var client = _clientStatusRepository.GetClient(clientName, instance);
        if (client == null)
            throw new KeyNotFoundException();

        var session = client.RunSessions.FirstOrDefault(a => a.RunSessionId == updatedConfiguration.RunSessionId);
        if (session is null)
            throw new KeyNotFoundException();

        session.LiveReload = updatedConfiguration.LiveReload;
        if (updatedConfiguration.PollIntervalMs is not null)
            session.PollIntervalMs = updatedConfiguration.PollIntervalMs.Value;
        session.RestartRequested = updatedConfiguration.RestartRequested;

        _clientStatusRepository.UpdateClientStatus(client);
        return updatedConfiguration;
    }

    public List<ClientStatusDataContract> GetAll()
    {
        var clients = _clientStatusRepository.GetAllClients();
        return clients.Select(a => _clientStatusConverter.Convert(a))
            .Where(a => a.RunSessions.Any())
            .ToList();
    }

    public void SetRequesterDetails(string? ipAddress, string? hostname)
    {
        _requestIpAddress = ipAddress;
        _requesterHostname = hostname;
    }

    private async Task RemoveExpiredSessions(ClientStatusBusinessEntity client)
    {
        foreach (var session in client.RunSessions.ToList())
        {
            _logger.LogInformation(
                $"{session.Id}. Last seen:{session.LastSeen}. Poll interval: {session.PollIntervalMs}");
            if (session.IsExpired())
            {
                client.RunSessions.Remove(session);
                _eventLogRepository.Add(_eventLogFactory.ExpiredSession(session, client));
                await _webHookDisseminationService.ClientDisconnected(session, client);
            }
        }
    }
    
    private async Task HandleConfigurationErrorStatusChanged(StatusRequestDataContract statusRequest,
        ClientStatusBusinessEntity client)
    {
        _eventLogRepository.Add(_eventLogFactory.ConfigurationErrorStatusChanged(client, statusRequest));

        foreach (var configurationError in statusRequest.ConfigurationErrors)
            _eventLogRepository.Add(_eventLogFactory.ConfigurationError(client, configurationError));

        await _webHookDisseminationService.ConfigurationErrorStatusChanged(client, statusRequest);
    }
}