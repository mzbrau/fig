// using Fig.Api.ExtensionMethods;
using System.Diagnostics;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
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
        ISettingHistoryRepository settingHistoryRepository)
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

        // Handle externally managed settings reported by the client
        if (statusRequest.ExternallyManagedSettings is { Count: > 0 })
        {
            await ProcessExternallyManagedSettings(clientName, instance, statusRequest.ExternallyManagedSettings, session);
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

    private async Task ProcessExternallyManagedSettings(
        string clientName, 
        string? instance, 
        List<ExternallyManagedSettingDataContract> externallyManagedSettings,
        ClientRunSessionBusinessEntity session)
    {
        _logger.LogInformation(
            "Processing {Count} externally managed settings from client {ClientName} instance {Instance} run session {RunSessionId}: {SettingNames}",
            externallyManagedSettings.Count,
            clientName.Sanitize(),
            instance,
            session.RunSessionId,
            string.Join(", ", externallyManagedSettings.Select(s => s.Name)));

        var settingClient = await _settingClientRepository.GetClient(clientName, instance);
        if (settingClient is null)
        {
            _logger.LogWarning("Could not find setting client for {ClientName} instance {Instance}", clientName.Sanitize(), instance);
            return;
        }

        var timeOfUpdate = DateTime.UtcNow;
        var updatedAny = false;

        foreach (var externalSetting in externallyManagedSettings)
        {
            // Find matching setting - remove colons from name to match the setting name format
            var settingName = externalSetting.Name.Split(':').LastOrDefault() ?? externalSetting.Name;
            var setting = settingClient.Settings.FirstOrDefault(s => 
                string.Equals(s.Name, settingName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Name, externalSetting.Name, StringComparison.OrdinalIgnoreCase));

            if (setting is null)
            {
                _logger.LogDebug(
                    "Setting {SettingName} not found in client {ClientName}, skipping externally managed update",
                    externalSetting.Name, clientName.Sanitize());
                continue;
            }

            // If setting is already externally managed, skip (latching behavior)
            if (setting.IsExternallyManaged)
            {
                _logger.LogDebug(
                    "Setting {SettingName} is already externally managed, skipping",
                    setting.Name);
                continue;
            }

            // Mark as externally managed (latching - can only be undone via value only import)
            setting.IsExternallyManaged = true;

            // Update the value if different
            var newValueString = externalSetting.Value?.ToString();
            var currentValueString = setting.Value?.GetValue()?.ToString();
            
            if (!string.Equals(newValueString, currentValueString, StringComparison.Ordinal) && externalSetting.Value is not null)
            {
                var originalValue = setting.Value;
                
                try
                {
                    // Create the new value using the setting converter
                    var dataContract = ValueDataContractFactory.CreateContract(externalSetting.Value, setting.ValueType ?? typeof(object));
                    var newValue = _settingConverter.Convert(dataContract);
                    
                    setting.Value = newValue;
                    setting.LastChanged = timeOfUpdate;

                    // Record in history
                    var displayValue = setting.IsSecret ? "******" : newValueString;
                    await _settingHistoryRepository.Add(new SettingValueBusinessEntity
                    {
                        ClientId = settingClient.Id,
                        ChangedAt = timeOfUpdate,
                        SettingName = setting.Name,
                        Value = newValue,
                        ChangedBy = $"EXTERNAL PROVIDER (RunSession: {session.RunSessionId})"
                    });

                    // Log the event
                    await _eventLogRepository.Add(_eventLogFactory.SettingValueUpdateByExternalProvider(
                        settingClient, 
                        setting.Name, 
                        originalValue?.GetValue()?.ToString(), 
                        displayValue,
                        session.RunSessionId));

                    _logger.LogInformation(
                        "Setting {SettingName} in client {ClientName} marked as externally managed and updated",
                        setting.Name, clientName.Sanitize());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update externally managed setting {SettingName}", setting.Name);
                    continue;
                }
            }
            else
            {
                // Just mark as externally managed without value change
                await _eventLogRepository.Add(_eventLogFactory.SettingMarkedAsExternallyManaged(
                    settingClient, 
                    setting.Name,
                    session.RunSessionId));
                
                _logger.LogInformation(
                    "Setting {SettingName} in client {ClientName} marked as externally managed (value unchanged)",
                    setting.Name, clientName.Sanitize());
            }

            updatedAny = true;
        }

        if (updatedAny)
        {
            settingClient.LastSettingValueUpdate = timeOfUpdate;
            await _settingClientRepository.UpdateClient(settingClient);
        }
    }
}