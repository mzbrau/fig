using System.Diagnostics;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Validators;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Health;
using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class StatusService : AuthenticatedService, IStatusService
{
    private readonly IClientStatusConverter _clientStatusConverter;
    private readonly IClientStatusRepository _clientStatusRepository;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<StatusService> _logger;
    private readonly IWebHookDisseminationService _webHookDisseminationService;
    private readonly IClientRunSessionRepository _clientRunSessionRepository;
    private string? _requesterHostname;
    private string? _requestIpAddress;

    public StatusService(
        IClientStatusRepository clientStatusRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IClientStatusConverter clientStatusConverter,
        IConfigurationRepository configurationRepository,
        ILogger<StatusService> logger,
        IWebHookDisseminationService webHookDisseminationService,
        IClientRunSessionRepository clientRunSessionRepository)
    {
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _clientStatusConverter = clientStatusConverter;
        _configurationRepository = configurationRepository;
        _logger = logger;
        _webHookDisseminationService = webHookDisseminationService;
        _clientRunSessionRepository = clientRunSessionRepository;
    }

    public async Task<StatusResponseDataContract> SyncStatus(
        string clientName,
        string? instance,
        string clientSecret,
        StatusRequestDataContract statusRequest)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var client = await _clientStatusRepository.GetClient(clientName, instance);

        if (client is null && !string.IsNullOrEmpty(instance))
            client = await _clientStatusRepository.GetClient(clientName);
        
        if (client is null)
            throw new KeyNotFoundException($"No existing registration for client '{clientName}'");

        var registrationStatus = RegistrationStatusValidator.GetStatus(client, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
            throw new UnauthorizedAccessException();

        await RemoveExpiredSessions(client);
        var configuration = await _configurationRepository.GetConfiguration();
        
        var session = client.RunSessions.FirstOrDefault(a => a.RunSessionId == statusRequest.RunSessionId);

        bool healthChanged;
        var originalStatus = session?.HealthStatus ?? FigHealthStatus.Unknown;
        if (session is not null)
        {
            if (session.HasConfigurationError != statusRequest.HasConfigurationError)
                await HandleConfigurationErrorStatusChanged(statusRequest, client);
            
            healthChanged = session.Update(statusRequest, _requesterHostname, _requestIpAddress, configuration);
        }
        else
        {
            _logger.LogInformation("Creating new run session for client {ClientName} with id {RunSessionId}. StartTime:{StartTime}", clientName, statusRequest.RunSessionId, statusRequest.StartTime);
            session = new ClientRunSessionBusinessEntity
            {
                RunSessionId = statusRequest.RunSessionId,
                StartTimeUtc = statusRequest.StartTime,
                LiveReload = true,
                PollIntervalMs = statusRequest.PollIntervalMs,
                LastSettingLoadUtc = DateTime.UtcNow // Assume it loaded settings on startup.
            };
            healthChanged = session.Update(statusRequest, _requesterHostname, _requestIpAddress, configuration);
            client.RunSessions.Add(session);
            await _eventLogRepository.Add(_eventLogFactory.NewSession(session, client));
            if (statusRequest.HasConfigurationError)
                await HandleConfigurationErrorStatusChanged(statusRequest, client);
            await _webHookDisseminationService.ClientConnected(session, client);
        }
        
        await _clientStatusRepository.UpdateClientStatus(client);
        
        if (healthChanged)
        {
            var healthDetails = JsonConvert.DeserializeObject<HealthDataContract>(session.HealthReportJson!, JsonSettings.FigDefault);
            await _eventLogRepository.Add(_eventLogFactory.HealthStatusChanged(session, client, healthDetails!, originalStatus));
            await _webHookDisseminationService.HealthStatusChanged(session, client, healthDetails!);
        }

        var updateAvailable = session.LiveReload && client.LastSettingValueUpdate > statusRequest.LastSettingUpdate;
        var changedSettings = await GetChangedSettingNames(updateAvailable,
            statusRequest.LastSettingUpdate,
            client.LastSettingValueUpdate ?? DateTime.MinValue,
            client.Name,
            client.Instance);

        var pollIntervalOverride = configuration.PollIntervalOverride;
        
        return new StatusResponseDataContract
        {
            SettingUpdateAvailable = updateAvailable,
            PollIntervalMs = pollIntervalOverride ?? session.PollIntervalMs,
            AllowOfflineSettings = configuration.AllowOfflineSettings,
            RestartRequested = session.RestartRequested,
            ChangedSettings = changedSettings,
        };
    }

    public async Task SetLiveReload(Guid runSessionId, bool liveReload)
    {
        var runSession = await _clientRunSessionRepository.GetRunSession(runSessionId);
        if (runSession is null)
            throw new KeyNotFoundException($"No run session registration for run session id {runSessionId}");

        var originalValue = runSession.LiveReload;
        
        runSession.LiveReload = liveReload;
        await _clientRunSessionRepository.UpdateRunSession(runSession);
        
        await _eventLogRepository.Add(_eventLogFactory.LiveReloadChange(runSession, originalValue, AuthenticatedUser));
    }
    
    public async Task RequestRestart(Guid runSessionId)
    {
        var runSession = await _clientRunSessionRepository.GetRunSession(runSessionId);
        if (runSession is null)
            throw new KeyNotFoundException($"No run session registration for run session id {runSessionId}");
        
        runSession.RestartRequested = true;
        await _clientRunSessionRepository.UpdateRunSession(runSession);
        
        await _eventLogRepository.Add(_eventLogFactory.RestartRequested(runSession, AuthenticatedUser));
    }
    
    public async Task<List<ClientStatusDataContract>> GetAll()
    {
        var clients = await _clientStatusRepository.GetAllClients(AuthenticatedUser);
        return clients.Select(a => _clientStatusConverter.Convert(a))
            .Where(a => a.RunSessions.Any())
            .ToList();
    }

    public void SetRequesterDetails(string? ipAddress, string? hostname)
    {
        _requestIpAddress = ipAddress;
        _requesterHostname = hostname;
    }

    public async Task MarkRestartRequired(string clientName, string? instance)
    {
        var client = await _clientStatusRepository.GetClient(clientName, instance);
        if (client == null)
            throw new KeyNotFoundException($"No existing registration for client '{clientName}'");

        foreach (var runSession in client.RunSessions)
        {
            runSession.RestartRequiredToApplySettings = true;
        }
        
        await _clientStatusRepository.UpdateClientStatus(client);
    }

    private async Task<List<string>?> GetChangedSettingNames(bool updateAvailable, DateTime startTime, DateTime endTime, string clientName, string? instance)
    {
        if (!updateAvailable)
            return null;

        var start = DateTime.SpecifyKind(startTime.AddSeconds(-1), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(endTime.AddSeconds(1), DateTimeKind.Utc);
        var valueChangeLogs = await _eventLogRepository.GetSettingChanges(start, end, clientName, instance);
        return valueChangeLogs
            .Where(a => a.SettingName is not null)
            .Select(a => a.SettingName!)
            .Distinct()
            .ToList();
    }

    private async Task RemoveExpiredSessions(ClientStatusBusinessEntity client)
    {
        foreach (var session in client.RunSessions.ToList())
        {
            _logger.LogTrace("{SessionId}. Last seen:{SessionLastSeen}. Poll interval: {SessionPollIntervalMs}", session.Id, session.LastSeen, session.PollIntervalMs);
            if (session.IsExpired())
            {
                _logger.LogInformation("Removing expired session {RunSessionId} for client {ClientName}", session.RunSessionId, client.Name);
                client.RunSessions.Remove(session);
                await _eventLogRepository.Add(_eventLogFactory.ExpiredSession(session, client));
                await _webHookDisseminationService.ClientDisconnected(session, client);
            }
        }
    }
    
    private async Task HandleConfigurationErrorStatusChanged(StatusRequestDataContract statusRequest,
        ClientStatusBusinessEntity client)
    {
        await _eventLogRepository.Add(_eventLogFactory.ConfigurationErrorStatusChanged(client, statusRequest));

        foreach (var configurationError in statusRequest.ConfigurationErrors)
            await _eventLogRepository.Add(_eventLogFactory.ConfigurationError(client, configurationError));

        await _webHookDisseminationService.ConfigurationErrorStatusChanged(client, statusRequest);
    }
}