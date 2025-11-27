// using Fig.Api.ExtensionMethods;
using System.Diagnostics;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
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
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingConverter _settingConverter;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly ISettingChangeRepository _settingChangeRepository;
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
        IClientRunSessionRepository clientRunSessionRepository,
        ISettingClientRepository settingClientRepository,
        ISettingConverter settingConverter,
        ISettingHistoryRepository settingHistoryRepository,
        ISettingChangeRepository settingChangeRepository)
    {
        _clientStatusRepository = clientStatusRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _clientStatusConverter = clientStatusConverter;
        _configurationRepository = configurationRepository;
        _logger = logger;
        _webHookDisseminationService = webHookDisseminationService;
        _clientRunSessionRepository = clientRunSessionRepository;
        _settingClientRepository = settingClientRepository;
        _settingConverter = settingConverter;
        _settingHistoryRepository = settingHistoryRepository;
        _settingChangeRepository = settingChangeRepository;
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
        {
            await _eventLogRepository.Add(_eventLogFactory.InvalidClientSecretAttempt(client.Name, "sync status",  _requestIpAddress, _requesterHostname));
            throw new UnauthorizedAccessException($"Invalid Secret for client '{client.Name}'");
        }

        var configuration = await _configurationRepository.GetConfiguration();
        
        var session = client.RunSessions.FirstOrDefault(a => a.RunSessionId == statusRequest.RunSessionId);

        bool healthChanged;
        var originalStatus = session?.HealthStatus ?? FigHealthStatus.Unknown;
        if (session is not null)
        {
            healthChanged = session.Update(statusRequest, _requesterHostname, _requestIpAddress, configuration);
        }
        else
        {
            _logger.LogInformation("Creating new run session for client {ClientName} with id {RunSessionId}. StartTime:{StartTime}", clientName.Sanitize(), statusRequest.RunSessionId, statusRequest.StartTime);
            session = new ClientRunSessionBusinessEntity
            {
                RunSessionId = statusRequest.RunSessionId,
                StartTimeUtc = statusRequest.StartTime,
                LiveReload = true,
                PollIntervalMs = statusRequest.PollIntervalMs,
                LastSettingLoadUtc = DateTime.UtcNow, // Assume it loaded settings on startup.
                InstanceName = instance
            };
            healthChanged = session.Update(statusRequest, _requesterHostname, _requestIpAddress, configuration);
            client.RunSessions.Add(session);
            await _eventLogRepository.Add(_eventLogFactory.NewSession(session, client));
            await _webHookDisseminationService.ClientConnected(session, client);
        }
        
        await _clientStatusRepository.UpdateClientStatus(client);
        
        if (healthChanged)
        {
            var healthDetails = JsonConvert.DeserializeObject<HealthDataContract>(session.HealthReportJson!, JsonSettings.FigDefault);
            await _eventLogRepository.Add(_eventLogFactory.HealthStatusChanged(session, client, healthDetails!, originalStatus));
            await _webHookDisseminationService.HealthStatusChanged(session, client, healthDetails!);
        }

        // Handle externally managed settings
        if (statusRequest.ExternallyManagedSettings?.Count > 0)
        {
            await ProcessExternallyManagedSettings(clientName, instance, statusRequest.RunSessionId, statusRequest.ExternallyManagedSettings);
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

    private async Task ProcessExternallyManagedSettings(
        string clientName,
        string? instance,
        Guid runSessionId,
        List<ExternallyManagedSettingDataContract> externallyManagedSettings)
    {
        _logger.LogInformation(
            "Processing {Count} externally managed setting(s) for client {ClientName}",
            externallyManagedSettings.Count,
            clientName.Sanitize());

        var settingClient = await _settingClientRepository.GetClient(clientName, instance);
        if (settingClient == null && !string.IsNullOrEmpty(instance))
            settingClient = await _settingClientRepository.GetClient(clientName);

        if (settingClient == null)
        {
            _logger.LogWarning(
                "Cannot process externally managed settings: client {ClientName} not found",
                clientName.Sanitize());
            return;
        }

        var timeOfUpdate = DateTime.UtcNow;
        var changes = new List<ChangedSetting>();
        var anyChanges = false;

        foreach (var externalSetting in externallyManagedSettings)
        {
            var setting = settingClient.Settings.FirstOrDefault(s => s.Name == externalSetting.Name);
            if (setting == null)
            {
                _logger.LogWarning(
                    "Externally managed setting {SettingName} not found in client {ClientName}",
                    externalSetting.Name,
                    clientName.Sanitize());
                continue;
            }

            // Mark as externally managed (latching - can only be unset via value-only import)
            if (!setting.IsExternallyManaged)
            {
                setting.IsExternallyManaged = true;
                anyChanges = true;
                _logger.LogInformation(
                    "Setting {SettingName} for client {ClientName} marked as externally managed",
                    externalSetting.Name,
                    clientName.Sanitize());
            }

            // Update the value if it differs
            var newValue = externalSetting.Value;
            if (newValue != null)
            {
                try
                {
                    var dataContract = ValueDataContractFactory.CreateContract(newValue, setting.ValueType ?? typeof(object));
                    var convertedValue = _settingConverter.Convert(dataContract);
                    var originalValueJson = JsonConvert.SerializeObject(setting.Value, JsonSettings.FigDefault);
                    var newValueJson = JsonConvert.SerializeObject(convertedValue, JsonSettings.FigDefault);

                    if (originalValueJson != newValueJson)
                    {
                        var dataGridDefinition = setting.GetDataGridDefinition();
                        changes.Add(new ChangedSetting(
                            setting.Name,
                            setting.Value,
                            convertedValue,
                            setting.IsSecret,
                            dataGridDefinition,
                            true)); // setting is externally managed
                        
                        setting.Value = convertedValue;
                        setting.LastChanged = timeOfUpdate;
                        anyChanges = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to convert externally managed setting value for {SettingName}",
                        externalSetting.Name);
                }
            }
        }

        if (anyChanges)
        {
            settingClient.LastSettingValueUpdate = timeOfUpdate;
            await _settingClientRepository.UpdateClient(settingClient);
            
            // Record changes with appropriate user context
            var userName = $"ConfigurationProvider@{_requesterHostname ?? "Unknown"}:{runSessionId}";
            foreach (var change in changes)
            {
                await _eventLogRepository.Add(_eventLogFactory.SettingValueUpdate(
                    settingClient.Id,
                    settingClient.Name,
                    settingClient.Instance,
                    change.Name,
                    change.OriginalValue?.GetValue(),
                    change.NewValue?.GetValue(),
                    "Value overridden by external configuration provider",
                    timeOfUpdate,
                    userName));

                await _settingHistoryRepository.Add(new SettingValueBusinessEntity
                {
                    ClientId = settingClient.Id,
                    SettingName = change.Name,
                    Value = change.NewValue,
                    ChangedAt = timeOfUpdate,
                    ChangedBy = userName
                });
            }

            await _settingChangeRepository.RegisterChange();
            
            _logger.LogInformation(
                "Applied {Count} externally managed setting change(s) for client {ClientName}",
                changes.Count,
                clientName.Sanitize());
        }
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
}