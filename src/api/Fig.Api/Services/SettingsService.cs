using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Fig.Api.Constants;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Secrets;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Common.Constants;
using Fig.Common.Events;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingMigrations;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class SettingsService : AuthenticatedService, ISettingsService
{
    private const string MigrateFromHistoryChangedBy = "MIGRATE_FROM";
    private const long SlowRegistrationStepWarningMs = 1000;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<SettingsService> _logger;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingConverter _settingConverter;
    private readonly ISettingDefinitionConverter _settingDefinitionConverter;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly IValidValuesHandler _validValuesHandler;
    private readonly IDeferredClientImportRepository _deferredClientImportRepository;
    private readonly ISettingChangeRepository _settingChangeRepository;
    private readonly ISettingApplier _settingApplier;
    private readonly ISettingChangeRecorder _settingChangeRecorder;
    private readonly IWebHookDisseminationService _webHookDisseminationService;
    private readonly IStatusService _statusService;
    private readonly ISecretStoreHandler _secretStoreHandler;
    private readonly IEventDistributor _eventDistributor;
    private readonly IDeferredChangeRepository _deferredChangeRepository;
    private readonly IClientRegistrationLockService _clientRegistrationLockService;
    private readonly IRegistrationStatusValidator _registrationStatusValidator;
    private readonly IClientRegistrationHistoryService _clientRegistrationHistoryService;
    private readonly ISettingGroupService _settingGroupService;
    private readonly IClientOverrideService _clientOverrideService;
    private string? _requesterHostname;
    private string? _requestIpAddress;

    public SettingsService(ILogger<SettingsService> logger,
        ISettingClientRepository settingClientRepository,
        IEventLogRepository eventLogRepository,
        ISettingHistoryRepository settingHistoryRepository,
        IClientOverrideService clientOverrideService,
        ISettingConverter settingConverter,
        ISettingDefinitionConverter settingDefinitionConverter,
        IEventLogFactory eventLogFactory,
        IConfigurationRepository configurationRepository,
        IValidValuesHandler validValuesHandler,
        IDeferredClientImportRepository deferredClientImportRepository,
        ISettingChangeRepository settingChangeRepository,
        ISettingApplier settingApplier,
        ISettingChangeRecorder settingChangeRecorder,
        IWebHookDisseminationService webHookDisseminationService,
        IStatusService statusService,
        ISecretStoreHandler secretStoreHandler,
        IEventDistributor eventDistributor,
        IDeferredChangeRepository deferredChangeRepository,
        IClientRegistrationLockService clientRegistrationLockService,
        IRegistrationStatusValidator registrationStatusValidator,
        IClientRegistrationHistoryService clientRegistrationHistoryService,
        ISettingGroupService settingGroupService)
    {
        _logger = logger;
        _settingClientRepository = settingClientRepository;
        _eventLogRepository = eventLogRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _clientOverrideService = clientOverrideService;
        _settingConverter = settingConverter;
        _settingDefinitionConverter = settingDefinitionConverter;
        _eventLogFactory = eventLogFactory;
        _configurationRepository = configurationRepository;
        _validValuesHandler = validValuesHandler;
        _deferredClientImportRepository = deferredClientImportRepository;
        _settingChangeRepository = settingChangeRepository;
        _settingApplier = settingApplier;
        _settingChangeRecorder = settingChangeRecorder;
        _webHookDisseminationService = webHookDisseminationService;
        _statusService = statusService;
        _secretStoreHandler = secretStoreHandler;
        _eventDistributor = eventDistributor;
        _deferredChangeRepository = deferredChangeRepository;
        _clientRegistrationLockService = clientRegistrationLockService;
        _registrationStatusValidator = registrationStatusValidator;
        _clientRegistrationHistoryService = clientRegistrationHistoryService;
        _settingGroupService = settingGroupService;
    }

    public async Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract client)
    {
        var debugEnabled = _logger.IsEnabled(LogLevel.Debug);
        if (debugEnabled) _logger.LogDebug("Acquiring registration lock for client {ClientName}", client.Name.Sanitize());
        var lockSw = debugEnabled ? Stopwatch.StartNew() : null;
        using var lockHandle = await _clientRegistrationLockService.AcquireLockAsync(client.Name);
        if (debugEnabled) _logger.LogDebug("Registration lock acquired for client {ClientName} in {ElapsedMs} ms", client.Name.Sanitize(), lockSw!.ElapsedMilliseconds);
        await RegisterSettingsInternal(clientSecret, client);
    }

    public async Task<List<SettingMigrationRequestDataContract>> GetMigrateFromMigrationRequests(
        string clientSecret,
        SettingsClientDefinitionDataContract clientDefinition)
    {
        using var lockHandle = await _clientRegistrationLockService.AcquireLockAsync(clientDefinition.Name);

        var configuration = await _configurationRepository.GetConfiguration();
        if (!configuration.AllowMigrateFromMigrations)
        {
            _logger.LogInformation("MigrateFrom migrations are disabled in server configuration. Skipping migration preview for client {ClientName}", clientDefinition.Name.Sanitize());
            return [];
        }

        var existingRegistrations = (await _settingClientRepository.GetAllInstancesOfClient(clientDefinition.Name, false)).ToList();
        if (!existingRegistrations.Any())
            return [];

        var registrationStatus = _registrationStatusValidator.GetStatus(existingRegistrations, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
        {
            await _eventLogRepository.Add(_eventLogFactory.InvalidClientSecretAttempt(
                clientDefinition.Name,
                "preview migrate from migrations",
                _requestIpAddress,
                _requesterHostname));
            throw new UnauthorizedAccessException(
                $"Settings for client '{clientDefinition.Name}' have already been registered with a different secret.");
        }

        var updatedSettingDefinitions = _settingDefinitionConverter.Convert(clientDefinition);
        var customMigrationTargets = updatedSettingDefinitions.Settings
            .Where(setting => !string.IsNullOrWhiteSpace(setting.MigrateFrom) &&
                              !string.IsNullOrWhiteSpace(setting.MigrateFromMigrationMethod))
            .ToList();

        if (!customMigrationTargets.Any())
            return [];

        var result = new List<SettingMigrationRequestDataContract>();
        foreach (var registration in existingRegistrations)
        {
            foreach (var targetSetting in customMigrationTargets)
            {
                if (registration.Settings.Any(setting => setting.Name == targetSetting.Name))
                    continue;

                var sourceSetting = registration.Settings.FirstOrDefault(setting => setting.Name == targetSetting.MigrateFrom);
                if (sourceSetting is null)
                    continue;

                if (sourceSetting.IsSecret && !targetSetting.IsSecret)
                {
                    throw new InvalidOperationException(
                        $"Custom MigrateFrom migration from secret setting '{sourceSetting.Name}' " +
                        $"to non-secret setting '{targetSetting.Name}' is not allowed.");
                }

                if (sourceSetting.IsSecret)
                    await _secretStoreHandler.HydrateSecret(registration, sourceSetting.Name);

                result.Add(new SettingMigrationRequestDataContract(
                    sourceSetting.Name,
                    targetSetting.Name,
                    registration.Instance,
                    sourceSetting.ValueType,
                    targetSetting.ValueType,
                    _settingConverter.Convert(
                        sourceSetting.Value,
                        sourceSetting.HasSchema(),
                        sourceSetting.GetDataGridDefinition()),
                    sourceSetting.IsSecret,
                    targetSetting.IsSecret,
                    ComputeMigrationFingerprint(sourceSetting)));
            }
        }

        return result;
    }

    private async Task RegisterSettingsInternal(string clientSecret, SettingsClientDefinitionDataContract client)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var debugEnabled = _logger.IsEnabled(LogLevel.Debug);
        var sanitizedClientName = client.Name.Sanitize();
        var totalSw = Stopwatch.StartNew();
        var stepSw = Stopwatch.StartNew();

        var configuration = await _configurationRepository.GetConfiguration();
        if (debugEnabled) _logger.LogDebug("GetConfiguration completed in {ElapsedMs} ms for client {ClientName}", stepSw.ElapsedMilliseconds, sanitizedClientName);
        LogSlowRegistrationStep("GetConfiguration", stepSw.ElapsedMilliseconds, sanitizedClientName);

        if (!configuration.AllowNewRegistrations)
        {
            _logger.LogWarning("Registration of client {ClientName} blocked as registrations are disabled", sanitizedClientName);
            throw new UnauthorizedAccessException("New registrations are currently disabled");
        }

        _logger.LogInformation("Processing registration from client {ClientName} and instance '{Instance}'", sanitizedClientName, client.Instance);

        stepSw.Restart();
        var existingRegistrations = (await _settingClientRepository.GetAllInstancesOfClient(client.Name, false)).ToList();
        if (debugEnabled) _logger.LogDebug("GetAllInstancesOfClient completed in {ElapsedMs} ms for client {ClientName}, found {Count} existing registrations", stepSw.ElapsedMilliseconds, sanitizedClientName, existingRegistrations.Count);
        LogSlowRegistrationStep("GetAllInstancesOfClient", stepSw.ElapsedMilliseconds, sanitizedClientName);

        var registrationStatus = _registrationStatusValidator.GetStatus(existingRegistrations, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
        {
            await _eventLogRepository.Add(_eventLogFactory.InvalidClientSecretAttempt(client.Name, "register settings",  _requestIpAddress, _requesterHostname));
            throw new UnauthorizedAccessException(
                $"Settings for client '{client.Name}' have already been registered with a different secret.");
        }

        stepSw.Restart();
        var clientBusinessEntity = _settingDefinitionConverter.Convert(client);
        if (debugEnabled) _logger.LogDebug("SettingDefinitionConverter.Convert completed in {ElapsedMs} ms for client {ClientName}", stepSw.ElapsedMilliseconds, sanitizedClientName);
        LogSlowRegistrationStep("SettingDefinitionConverter.Convert", stepSw.ElapsedMilliseconds, sanitizedClientName);

        if (!configuration.AllowMigrateFromMigrations)
        {
            client.SettingMigrationResults.Clear();
            foreach (var setting in clientBusinessEntity.Settings)
            {
                setting.MigrateFrom = null;
                setting.MigrateFromMigrationMethod = null;
            }
        }

        clientBusinessEntity.Settings.ToList().ForEach(a => a.Validate());

        clientBusinessEntity.LastRegistration = DateTime.UtcNow;

        // When description is empty (deferred registration), copy the existing description so the
        // equivalence check doesn't incorrectly treat every startup as a "definition changed" event.
        if (string.IsNullOrEmpty(clientBusinessEntity.Description) && existingRegistrations.Any())
            clientBusinessEntity.Description = existingRegistrations.First().Description;

        if (!existingRegistrations.Any())
        {
            if (debugEnabled) _logger.LogDebug("Starting initial registration for client {ClientName}", sanitizedClientName);
            stepSw.Restart();
            await HandleInitialRegistration(clientBusinessEntity, registrationStatus, clientSecret);
            if (debugEnabled) _logger.LogDebug("HandleInitialRegistration completed in {ElapsedMs} ms for client {ClientName}", stepSw.ElapsedMilliseconds, sanitizedClientName);
            LogSlowRegistrationStep("HandleInitialRegistration", stepSw.ElapsedMilliseconds, sanitizedClientName);
            try
            {
                stepSw.Restart();
                await _clientRegistrationHistoryService.RecordRegistration(client);
                if (debugEnabled) _logger.LogDebug("RecordRegistration (initial) completed in {ElapsedMs} ms for client {ClientName}", stepSw.ElapsedMilliseconds, sanitizedClientName);
                LogSlowRegistrationStep("RecordRegistration initial", stepSw.ElapsedMilliseconds, sanitizedClientName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record registration history for client {ClientName}", sanitizedClientName);
            }
        }
        else if (existingRegistrations.Any(x => x.HasEquivalentDefinitionTo(clientBusinessEntity)))
        {
            if (debugEnabled) _logger.LogDebug("Identical registration detected for client {ClientName}, short-circuiting", sanitizedClientName);
            stepSw.Restart();
            await RecordIdenticalRegistration(existingRegistrations);
            LogSlowRegistrationStep("RecordIdenticalRegistration", stepSw.ElapsedMilliseconds, sanitizedClientName);
        }
        else if (!configuration.AllowUpdatedRegistrations)
        {
            _logger.LogWarning(
                "Updated registration for client {ClientName} blocked as updated registrations are disabled", sanitizedClientName);
            throw new UnauthorizedAccessException("Updated registrations are currently disabled");
        }
        else
        {
            if (debugEnabled) _logger.LogDebug("Starting updated registration for client {ClientName}", sanitizedClientName);
            stepSw.Restart();
            await HandleUpdatedRegistration(clientBusinessEntity, existingRegistrations, client.SettingMigrationResults);
            if (debugEnabled) _logger.LogDebug("HandleUpdatedRegistration completed in {ElapsedMs} ms for client {ClientName}", stepSw.ElapsedMilliseconds, sanitizedClientName);
            LogSlowRegistrationStep("HandleUpdatedRegistration", stepSw.ElapsedMilliseconds, sanitizedClientName);
            try
            {
                stepSw.Restart();
                await _clientRegistrationHistoryService.RecordRegistration(client);
                if (debugEnabled) _logger.LogDebug("RecordRegistration (updated) completed in {ElapsedMs} ms for client {ClientName}", stepSw.ElapsedMilliseconds, sanitizedClientName);
                LogSlowRegistrationStep("RecordRegistration updated", stepSw.ElapsedMilliseconds, sanitizedClientName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record registration history for client {ClientName}", sanitizedClientName);
            }
        }

        stepSw.Restart();
        await ApplyClientSettingOverrides(client, existingRegistrations, configuration);
        if (debugEnabled) _logger.LogDebug("ApplyClientSettingOverrides completed in {ElapsedMs} ms for client {ClientName}", stepSw.ElapsedMilliseconds, sanitizedClientName);
        LogSlowRegistrationStep("ApplyClientSettingOverrides", stepSw.ElapsedMilliseconds, sanitizedClientName);

        totalSw.Stop();
        if (debugEnabled) _logger.LogDebug("RegisterSettingsInternal total duration: {ElapsedMs} ms for client {ClientName}", totalSw.ElapsedMilliseconds, sanitizedClientName);
        LogSlowRegistrationStep("RegisterSettingsInternal total", totalSw.ElapsedMilliseconds, sanitizedClientName);
    }

    private void LogSlowRegistrationStep(string step, long elapsedMs, string sanitizedClientName)
    {
        if (elapsedMs < SlowRegistrationStepWarningMs)
            return;

        _logger.LogWarning(
            "Slow registration step {Step} for client {ClientName} completed in {ElapsedMs} ms",
            step,
            sanitizedClientName,
            elapsedMs);
    }

    public async Task<SettingsClientLoadResult> GetAllClients()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity("GetAllClients");
        var loadResult = await _settingClientRepository.GetAllClientsBestEffort(AuthenticatedUser);

        var configuration = await _configurationRepository.GetConfiguration();

        var failures = loadResult.Failures
            .Select(failure => new ClientLoadFailureDataContract(
                failure.ClientName,
                failure.Instance,
                failure.SettingName,
                failure.Message))
            .ToList();

        var clients = new List<SettingsClientDefinitionDataContract>();
        using (Activity? convertActivity = ApiActivitySource.Instance.StartActivity("ConvertClients"))
        {
            var convertWatch = Stopwatch.StartNew();
            foreach (var client in loadResult.Clients)
            {
                try
                {
                    clients.Add(await _settingDefinitionConverter.Convert(
                        client,
                        configuration.AllowDisplayScripts,
                        AuthenticatedUser));
                }
                catch (Exception ex)
                {
                    failures.Add(new ClientLoadFailureDataContract(
                        client.Name,
                        client.Instance,
                        null,
                        "Client could not be converted and was omitted from this response."));
                    _logger.LogError(ex,
                        "Failed to convert client {ClientName} instance {Instance}. Client was omitted from this response.",
                        client.Name.Sanitize(),
                        client.Instance);
                }
            }

            convertActivity?.SetTag("fig.api.client_count", clients.Count);
            convertActivity?.SetTag("fig.api.setting_count", clients.Sum(c => c.Settings.Count));
            convertActivity?.SetTag("fig.api.elapsed_ms", convertWatch.ElapsedMilliseconds);
        }

        var resultClients = clients.Where(a => a.Settings.Any()).ToList();
        activity?.SetTag("fig.api.client_count", resultClients.Count);
        activity?.SetTag("fig.api.setting_count", resultClients.Sum(c => c.Settings.Count));
        activity?.SetTag("fig.api.load_failure_count", failures.Count);

        return new SettingsClientLoadResult(resultClients, failures);
    }

    public async Task<IEnumerable<SettingDataContract>> GetSettings(string clientName, string clientSecret, string? instance, Guid runSessionId)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var debugEnabled = _logger.IsEnabled(LogLevel.Debug);
        var sanitizedClientName = debugEnabled ? clientName.Sanitize() : null;
        var stepSw = debugEnabled ? Stopwatch.StartNew() : null;
        var existingRegistration = await _settingClientRepository.GetClient(clientName, instance);

        if (existingRegistration == null && !string.IsNullOrEmpty(instance))
            existingRegistration = await _settingClientRepository.GetClient(clientName);
        if (debugEnabled) _logger.LogDebug("GetClient completed in {ElapsedMs} ms for client {ClientName}", stepSw!.ElapsedMilliseconds, sanitizedClientName);

        if (existingRegistration == null)
            throw new KeyNotFoundException($"No existing registration for client '{clientName}'");

        var registrationStatus = _registrationStatusValidator.GetStatus(existingRegistration, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
        {
            await _eventLogRepository.Add(_eventLogFactory.InvalidClientSecretAttempt(clientName, "get settings",  _requestIpAddress, _requesterHostname));
            throw new UnauthorizedAccessException($"Invalid client secret for client '{clientName}'");
        }
        
        var session = existingRegistration.RunSessions.FirstOrDefault(a => a.RunSessionId == runSessionId);
        if (session is not null)
        {
            session.LastSettingLoadUtc = DateTime.UtcNow;
            stepSw?.Restart();
            await _settingClientRepository.UpdateClient(existingRegistration);
            if (debugEnabled) _logger.LogDebug("UpdateClient (LastSettingLoadUtc) completed in {ElapsedMs} ms for client {ClientName}", stepSw!.ElapsedMilliseconds, sanitizedClientName);
        }

        stepSw?.Restart();
        await _eventLogRepository.Add(_eventLogFactory.SettingsRead(existingRegistration.Id, clientName, instance));
        if (debugEnabled) _logger.LogDebug("EventLog SettingsRead completed in {ElapsedMs} ms for client {ClientName}", stepSw!.ElapsedMilliseconds, sanitizedClientName);

        stepSw?.Restart();
        await _secretStoreHandler.HydrateSecrets(existingRegistration);
        if (debugEnabled) _logger.LogDebug("HydrateSecrets completed in {ElapsedMs} ms for client {ClientName}", stepSw!.ElapsedMilliseconds, sanitizedClientName);

        var settingCount = existingRegistration.Settings.Count;
        if (debugEnabled) _logger.LogDebug("Returning {SettingCount} settings for client {ClientName}", settingCount, sanitizedClientName);
        return existingRegistration.Settings.Select(a => _settingConverter.Convert(a));
    }

    public async Task DeleteClient(string clientName, string? instance)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var client = await _settingClientRepository.GetClientForDeletion(clientName, instance);
        if (client != null)
        {
            await _settingClientRepository.DeleteClient(client);
            await _eventLogRepository.Add(_eventLogFactory.ClientDeleted(client.Id, clientName, instance, AuthenticatedUser));
            await _settingChangeRepository.RegisterChange();

            if (!await _settingClientRepository.HasAnyInstancesOfClient(clientName))
                await _settingGroupService.RemoveClientFromGroups(clientName);

            await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
                new CheckPointTrigger($"Client {client.Name.Sanitize()} deleted", AuthenticatedUser?.Username));
        }
    }

    public async Task UpdateSettingValues(string clientName, string? instance,
        SettingValueUpdatesDataContract updatedSettings, bool clientOverride = false)
    {
        var options = clientOverride
            ? SettingUpdateOptions.ClientOverride
            : SettingUpdateOptions.WebUser;

        await UpdateSettingValuesInternal(clientName, instance, updatedSettings, options);
    }

    public async Task UpdateSettingValuesFromClient(string clientName, string? instance, string clientSecret,
        SettingValueUpdatesDataContract updatedSettings)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        var client = await _settingClientRepository.GetClient(clientName, instance);
        if (client == null)
            throw new UnauthorizedAccessException($"Invalid client secret for client '{clientName}'");

        var registrationStatus = _registrationStatusValidator.GetStatus(client, clientSecret);
        if (registrationStatus is not (CurrentRegistrationStatus.MatchesExistingSecret or
            CurrentRegistrationStatus.IsWithinChangePeriodAndMatchesPreviousSecret))
        {
            await _eventLogRepository.Add(_eventLogFactory.InvalidClientSecretAttempt(clientName,
                "self-update settings",
                _requestIpAddress,
                _requesterHostname));
            throw new UnauthorizedAccessException($"Invalid client secret for client '{clientName}'");
        }

        await UpdateSettingValuesInternal(clientName, instance, updatedSettings, SettingUpdateOptions.ClientSelfUpdate, client);
    }

    private async Task UpdateSettingValuesInternal(string clientName, string? instance,
        SettingValueUpdatesDataContract updatedSettings, SettingUpdateOptions options,
        SettingClientBusinessEntity? existingClient = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity("UpdateSettingValues");
        var totalWatch = Stopwatch.StartNew();
        var isScheduled = updatedSettings.Schedule?.ApplyAtUtc != null;
        activity?.SetTag("fig.api.scheduled", isScheduled);
        
        if (options.RequireUserAccess)
            ThrowIfNoAccess(clientName);

        if (!options.AllowSchedule && updatedSettings.Schedule != null)
            throw new InvalidOperationException("Scheduled setting updates are not supported for client self-updates");

        if (isScheduled)
        {
            if (updatedSettings.Schedule!.RevertAtUtc.HasValue &&
                updatedSettings.Schedule.RevertAtUtc <= updatedSettings.Schedule.ApplyAtUtc)
                throw new InvalidOperationException("Revert time must be after apply time");
            
            await ScheduleChange(clientName, instance, updatedSettings, updatedSettings.Schedule.ApplyAtUtc!.Value, false);
            activity?.SetTag("fig.api.change_count", 0);
            activity?.SetTag("fig.api.elapsed_ms", totalWatch.ElapsedMilliseconds);
            return;
        }
        
        var dirty = false;
        SettingClientBusinessEntity? client;
        using (Activity? loadActivity = ApiActivitySource.Instance.StartActivity("LoadClient"))
        {
            var loadWatch = Stopwatch.StartNew();
            client = existingClient ?? await _settingClientRepository.GetClient(clientName, instance);
            loadActivity?.SetTag("fig.api.elapsed_ms", loadWatch.ElapsedMilliseconds);
        }

        if (client == null)
        {
            if (!options.CreateMissingClientOverride || string.IsNullOrWhiteSpace(instance))
                throw new KeyNotFoundException("Unknown client and instance combination");

            client = await _clientOverrideService.CreateClientOverride(clientName, instance, AuthenticatedUser);
            dirty = true;
        }

        activity?.SetTag("fig.api.setting_count", client.Settings.Count);
        
        var timeOfUpdate = DateTime.UtcNow;
        var valueUpdates = updatedSettings.ValueUpdates.ToList();
        bool restartRequired;
        List<ChangedSetting> changes;
        List<SettingDataContract> originalValues;

        using (Activity? applyActivity = ApiActivitySource.Instance.StartActivity("ApplyAndValidate"))
        {
            var applyWatch = Stopwatch.StartNew();
            if (options.RequireKnownSettings)
                ValidateClientUpdateSettings(client, valueUpdates);

            var updatedSettingBusinessEntities = valueUpdates.Select(dataContract =>
            {
                var originalSetting = client.Settings.FirstOrDefault(a => a.Name == dataContract.Name);
                var businessEntity = _settingConverter.Convert(dataContract, originalSetting);
                businessEntity.Serialize();
                return businessEntity;
            }).ToList();

            restartRequired = false;
            changes = new List<ChangedSetting>();
            originalValues = new List<SettingDataContract>();
            foreach (var updatedSetting in updatedSettingBusinessEntities)
            {
                var setting = client.Settings.FirstOrDefault(a => a.Name == updatedSetting.Name);

                if (setting != null && 
                    updatedSetting.ValueAsJson != JsonConvert.SerializeObject(setting.Value, JsonSettings.FigDefault))
                {
                    if (options.RequireUserClassification && !AuthenticatedUser.HasPermissionForClassification(setting))
                    {
                        throw new UnauthorizedAccessException(
                            $"User {AuthenticatedUser?.Username} does not have access to setting {setting.Name}");
                    }

                    if (updatedSettings.Schedule?.RevertAtUtc is not null)
                    {
                        originalValues.Add(_settingConverter.Convert(setting));
                    }
                    
                    var dataGridDefinition = setting.GetDataGridDefinition();
                    var originalValue = setting.Value;
                    setting.Value = _validValuesHandler.GetValue(updatedSetting.Value!,
                        setting.ValidValues, setting.ValueType ?? typeof(object), setting.LookupTableKey, dataGridDefinition);
                    setting.LastChanged = timeOfUpdate;
                    if (options.MarkChangedSettingsExternallyManaged)
                        setting.IsExternallyManaged = true;

                    changes.Add(new ChangedSetting(setting.Name, originalValue, setting.Value,
                        setting.IsSecret, dataGridDefinition, setting.IsExternallyManaged));
                    dirty = true;

                    if (!setting.SupportsLiveUpdate)
                        restartRequired = true;
                }
            }

            client.Settings.ToList().ForEach(a => a.Validate());
            applyActivity?.SetTag("fig.api.change_count", changes.Count);
            applyActivity?.SetTag("fig.api.elapsed_ms", applyWatch.ElapsedMilliseconds);
        }

        activity?.SetTag("fig.api.change_count", changes.Count);

        if (dirty)
        {
            if (originalValues.Any() && updatedSettings.Schedule?.RevertAtUtc is { } revertAt)
            {
                var original = new SettingValueUpdatesDataContract(originalValues, updatedSettings.ChangeMessage, null);
                await ScheduleChange(clientName, instance, original, revertAt, true);
            }

            using (Activity? secretActivity = ApiActivitySource.Instance.StartActivity("SecretStore"))
            {
                var secretWatch = Stopwatch.StartNew();
                await _secretStoreHandler.SaveSecrets(client, changes);
                await _secretStoreHandler.ClearSecrets(client);
                secretActivity?.SetTag("fig.api.elapsed_ms", secretWatch.ElapsedMilliseconds);
            }

            client.LastSettingValueUpdate = timeOfUpdate;

            using (Activity? updateActivity = ApiActivitySource.Instance.StartActivity("UpdateClient"))
            {
                var updateWatch = Stopwatch.StartNew();
                await _settingClientRepository.UpdateClient(client);
                updateActivity?.SetTag("fig.api.setting_count", client.Settings.Count);
                updateActivity?.SetTag("fig.api.elapsed_ms", updateWatch.ElapsedMilliseconds);
            }

            var user = options.Actor ?? AuthenticatedUser?.Username;
            var notificationUser = options.UseActorForNotifications ? user : AuthenticatedUser?.Username;

            using (Activity? recordActivity = ApiActivitySource.Instance.StartActivity("RecordSettingChanges"))
            {
                var recordWatch = Stopwatch.StartNew();
                await _settingChangeRecorder.RecordSettingChanges(changes, updatedSettings.ChangeMessage, timeOfUpdate, client, user);
                recordActivity?.SetTag("fig.api.change_count", changes.Count);
                recordActivity?.SetTag("fig.api.elapsed_ms", recordWatch.ElapsedMilliseconds);
            }

            using (Activity? sideEffectActivity = ApiActivitySource.Instance.StartActivity("WebHooksAndChangeStamp"))
            {
                var sideEffectWatch = Stopwatch.StartNew();
                await _webHookDisseminationService.SettingValueChanged(changes, client, notificationUser, updatedSettings.ChangeMessage);
                await _settingChangeRepository.RegisterChange();
                await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
                    new CheckPointTrigger($"Settings updated for client {client.Name.Sanitize()}", notificationUser));
                sideEffectActivity?.SetTag("fig.api.elapsed_ms", sideEffectWatch.ElapsedMilliseconds);
            }
            
            _logger.LogInformation("Updated settings for client {ClientName} with the following settings {SettingNames}",
                client.Name.Sanitize(), string.Join(", ", changes.Select(a => a.Name)));
        }
        else if (updatedSettings.Schedule?.RevertAtUtc is { } pendingRevertAt)
        {
            await EnsureRevertScheduledAfterPartialApply(
                clientName, instance, updatedSettings, client, valueUpdates, pendingRevertAt);
        }
        
        if (restartRequired)
        {
            using Activity? restartActivity = ApiActivitySource.Instance.StartActivity("MarkRestartRequired");
            var restartWatch = Stopwatch.StartNew();
            await _statusService.MarkRestartRequired(clientName, instance);
            restartActivity?.SetTag("fig.api.elapsed_ms", restartWatch.ElapsedMilliseconds);
        }

        activity?.SetTag("fig.api.elapsed_ms", totalWatch.ElapsedMilliseconds);
    }

    private static void ValidateClientUpdateSettings(SettingClientBusinessEntity client,
        IReadOnlyCollection<SettingDataContract> valueUpdates)
    {
        var duplicateNames = valueUpdates
            .GroupBy(a => a.Name, StringComparer.Ordinal)
            .Where(a => a.Count() > 1)
            .Select(a => a.Key)
            .ToList();

        if (duplicateNames.Any())
            throw new InvalidOperationException(
                $"Duplicate setting update(s) are not allowed: {string.Join(", ", duplicateNames)}");

        var existingSettingNames = client.Settings.Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        var unknownSettingNames = valueUpdates
            .Select(a => a.Name)
            .Where(a => !existingSettingNames.Contains(a))
            .ToList();

        if (unknownSettingNames.Any())
            throw new KeyNotFoundException(
                $"Unknown setting(s) for client '{client.Name}': {string.Join(", ", unknownSettingNames)}");
    }

    public async Task<IEnumerable<SettingValueDataContract>> GetSettingHistory(string clientName, string settingName,
        string? instance)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        // Use read-only since we only need to verify the client exists and has the setting
        var client = await _settingClientRepository.GetClientReadOnly(clientName, instance);

        if (client == null)
            throw new KeyNotFoundException("Unknown client and instance combination");

        if (client.Settings.All(a => a.Name != settingName))
            throw new KeyNotFoundException($"Client {clientName} does not have setting {settingName}");
        
        var history = await _settingHistoryRepository.GetAll(client.Id, settingName);
        return history.Select(a => _settingConverter.Convert(a));
    }

    public async Task<IEnumerable<ClientSettingsLastChangedDataContract>> GetLastChangedForAllClientsAndSettings()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        // Get all clients the authenticated user has access to (read-only, no lock)
        var clients = await _settingClientRepository.GetAllClients(AuthenticatedUser);
        var clientLookup = clients.ToDictionary(c => c.Id, c => (c.Name, c.Instance));

        // Single database query across all clients
        var allLastChanged = await _settingHistoryRepository.GetLastChangedForAllClients();

        // Group by ClientId and map to data contracts, filtering to accessible clients
        return allLastChanged
            .Where(sv => clientLookup.ContainsKey(sv.ClientId))
            .GroupBy(sv => sv.ClientId)
            .Select(group =>
            {
                var (name, instance) = clientLookup[group.Key];
                return new ClientSettingsLastChangedDataContract(
                    name,
                    instance,
                    group.Select(sv => _settingConverter.Convert(sv)).ToList());
            });
    }

    public async Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(string clientName,
        ClientSecretChangeRequestDataContract changeRequest)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var clients = (await _settingClientRepository.GetAllInstancesOfClient(clientName)).ToList();

        if (!clients.Any())
            throw new KeyNotFoundException("Unknown client");

        if (clients.Any(a => a.PreviousClientSecretExpiryUtc > DateTime.UtcNow))
            throw new InvalidClientSecretChangeException("Cannot change secret while previous secret is still valid");
        
        if (string.IsNullOrWhiteSpace(changeRequest.NewSecret))
            throw new ArgumentException("New secret cannot be null or empty.", "changeRequest.NewSecret");
        
        foreach (var client in clients)
        {
            var currentSecret = client.ClientSecret;
            client.ClientSecret = BCrypt.Net.BCrypt.EnhancedHashPassword(changeRequest.NewSecret);
            client.PreviousClientSecret = currentSecret;
            client.PreviousClientSecretExpiryUtc = changeRequest.OldSecretExpiryUtc;
            await _settingClientRepository.UpdateClient(client);
            await _eventLogRepository.Add(_eventLogFactory.ClientSecretChanged(client.Id,
                client.Name,
                client.Instance,
                AuthenticatedUser,
                changeRequest.OldSecretExpiryUtc));
        }
        
        await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
            new CheckPointTrigger($"Secret changed for client {clientName}", AuthenticatedUser?.Username));

        return new ClientSecretChangeResponseDataContract(clientName, changeRequest.OldSecretExpiryUtc);
    }

    public async Task<DateTime> GetLastSettingUpdate()
    {
        return (await _settingChangeRepository.GetLastChange())?.LastChange ?? DateTime.MinValue;
    }

    public async Task<ClientsDescriptionDataContract> GetClientDescriptions()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity("GetClientDescriptions");
        var stopwatch = Stopwatch.StartNew();
        var clientDescriptions = await _settingClientRepository.GetClientDescriptions(AuthenticatedUser);

        var clientDescriptionContracts = clientDescriptions.Select(client => 
            new ClientDescriptionDataContract(client.Name, client.Description))
            .ToList();

        activity?.SetTag("fig.api.client_count", clientDescriptionContracts.Count);
        activity?.SetTag("fig.api.elapsed_ms", stopwatch.ElapsedMilliseconds);

        return new ClientsDescriptionDataContract(clientDescriptionContracts);
    }

    public async Task UpdateClientDescription(string clientName, string? instance, string clientSecret, string description)
    {
        var client = await _settingClientRepository.GetClient(clientName, instance);
        if (client == null)
            return;

        var registrationStatus = _registrationStatusValidator.GetStatus(client, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
        {
            await _eventLogRepository.Add(_eventLogFactory.InvalidClientSecretAttempt(clientName, "update description", _requestIpAddress, _requesterHostname));
            throw new UnauthorizedAccessException($"Invalid client secret for client '{clientName}'");
        }

        client.Description = description;
        await _settingClientRepository.UpdateClient(client);

        const int maxDescriptionLogLength = 200;
        var descriptionSummary = description.Length > maxDescriptionLogLength
            ? $"{description[..maxDescriptionLogLength]}... [{description.Length} chars]"
            : description;
        await _eventLogRepository.Add(
            _eventLogFactory.ClientDescriptionUpdated(client.Id, client.Name, client.Instance, descriptionSummary));
    }

    public void SetRequesterDetails(string? ipAddress, string? hostname)
    {
        _requestIpAddress = ipAddress;
        _requesterHostname = hostname;
    }

    private async Task ApplyClientSettingOverrides(
        SettingsClientDefinitionDataContract client, 
        List<SettingClientBusinessEntity> existingRegistrations,
        FigConfigurationBusinessEntity configuration)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        if (ClientOverridesEnabledForClient() &&
            client.ClientSettingOverrides.Any())
        {
            var existingClient = existingRegistrations.FirstOrDefault(a => a.Instance == client.Instance);
            
            // Filter out overrides that haven't changed to avoid expensive update operations
            var changedOverrides = FilterChangedOverrides(client.ClientSettingOverrides.ToList(), existingClient);

            if (changedOverrides.Any())
            {
                var instanceInfo = string.IsNullOrEmpty(client.Instance) ? "" : $" (instance: {client.Instance})";
                var changeMessage = $"Client setting override from application '{client.Name}'{instanceInfo}";

                _logger.LogInformation(
                    "Applying setting override for client {ClientName} with the following settings {SettingNames}",
                    client.Name.Sanitize(), string.Join(", ", changedOverrides.Select(a => a.Name)));
                SetAuthenticatedUser(new ServiceUser());
                await UpdateSettingValues(
                    client.Name,
                    client.Instance,
                    new SettingValueUpdatesDataContract(changedOverrides, changeMessage),
                    true);
            }

            // Mark all overridden settings (both changed and unchanged) as externally managed
            var overrideSettingNames = client.ClientSettingOverrides.Select(a => a.Name).ToList();
            await MarkSettingsAsExternallyManaged(client.Name, client.Instance, overrideSettingNames);
        }
        
        bool ClientOverridesEnabledForClient() =>
            configuration.AllowClientOverrides && 
            (string.IsNullOrEmpty(configuration.ClientOverridesRegex) ||
             Regex.IsMatch(client.Name, configuration.ClientOverridesRegex!));
    }

    private async Task MarkSettingsAsExternallyManaged(string clientName, string? instance, List<string> settingNames)
    {
        var client = await _settingClientRepository.GetClient(clientName, instance);
        if (client == null)
            return;
        
        var modified = false;
        foreach (var settingName in settingNames)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == settingName);
            if (setting is { IsExternallyManaged: false })
            {
                setting.IsExternallyManaged = true;
                modified = true;
            }
        }
        
        if (modified)
        {
            await _settingClientRepository.UpdateClient(client);
        }
    }

    private List<RenamedSettingHistoryMigration> UpdateRegistrationsWithNewDefinitions(
        SettingClientBusinessEntity updatedSettingDefinitions,
        List<SettingClientBusinessEntity> existingRegistrations,
        List<SettingMigrationResultDataContract> migrationResults,
        Dictionary<Guid, List<ChangedSetting>> secretChanges)
    {
        var settingValues = existingRegistrations.ToDictionary(
            a => a.Instance ?? "Default",
            b => b.Settings.ToList());
        var renamedHistoryMigrations = new List<RenamedSettingHistoryMigration>();

        foreach (var registration in existingRegistrations)
        {
            if (!string.IsNullOrEmpty(updatedSettingDefinitions.Description))
                registration.Description = updatedSettingDefinitions.Description;
            registration.Settings.Clear();
            var values = settingValues[registration.Instance ?? "Default"];
            foreach (var setting in updatedSettingDefinitions.Settings)
            {
                var newSetting = setting.Clone();
                var matchingSetting = values.FirstOrDefault(a => a.Name == newSetting.Name);
                var isMigrateFromMatch = false;
                if (matchingSetting == null && !string.IsNullOrWhiteSpace(newSetting.MigrateFrom))
                {
                    matchingSetting = values.FirstOrDefault(a => a.Name == newSetting.MigrateFrom);
                    isMigrateFromMatch = matchingSetting != null;
                }

                var hasCustomMigrationMethod = !string.IsNullOrWhiteSpace(newSetting.MigrateFromMigrationMethod);
                var customMigrationResult = isMigrateFromMatch && matchingSetting is not null && hasCustomMigrationMethod
                    ? GetCustomMigrationResult(registration, matchingSetting, newSetting, migrationResults)
                    : null;

                if (matchingSetting != null && customMigrationResult is not null)
                {
                    ApplyCustomMigrationResult(newSetting, matchingSetting, customMigrationResult);
                    AddSecretChangeIfRequired(secretChanges, registration, newSetting, matchingSetting.Value);
                    AddRenamedHistoryMigrationIfRequired(
                        renamedHistoryMigrations,
                        updatedSettingDefinitions,
                        registration,
                        matchingSetting,
                        newSetting);
                }
                else if (matchingSetting != null &&
                         isMigrateFromMatch &&
                         hasCustomMigrationMethod)
                {
                    throw new InvalidOperationException(
                        $"Custom MigrateFrom migration result was not supplied for client '{updatedSettingDefinitions.Name}' " +
                        $"setting '{newSetting.Name}' from source '{matchingSetting.Name}'.");
                }
                else if (matchingSetting != null && matchingSetting.ValueType == newSetting.ValueType)
                {
                    newSetting.Value = matchingSetting.Value;
                    newSetting.LastChanged = matchingSetting.LastChanged;
                    if (isMigrateFromMatch)
                    {
                        AddSecretChangeIfRequired(secretChanges, registration, newSetting, matchingSetting.Value);
                        AddRenamedHistoryMigrationIfRequired(
                            renamedHistoryMigrations,
                            updatedSettingDefinitions,
                            registration,
                            matchingSetting,
                            newSetting);
                    }
                }
                else if (matchingSetting != null && isMigrateFromMatch)
                {
                    _logger.LogError(
                        "Unable to migrate setting value for client {ClientName} from {SourceSettingName} to {TargetSettingName} because the source type {SourceSettingType} does not match the target type {TargetSettingType}",
                        updatedSettingDefinitions.Name.Sanitize(),
                        matchingSetting.Name,
                        newSetting.Name,
                        matchingSetting.ValueType,
                        newSetting.ValueType);
                }
                else if (matchingSetting is null)
                {
                    AddSecretChangeIfRequired(secretChanges, registration, newSetting, null);
                }

                registration.Settings.Add(newSetting);
            }
        }

        return renamedHistoryMigrations;
    }

    private async Task HydrateSecretMigrateFromSources(
        SettingClientBusinessEntity updatedSettingDefinitions,
        List<SettingClientBusinessEntity> existingRegistrations)
    {
        var migrateFromTargets = updatedSettingDefinitions.Settings
            .Where(setting => !string.IsNullOrWhiteSpace(setting.MigrateFrom))
            .ToList();
        if (!migrateFromTargets.Any())
            return;

        foreach (var registration in existingRegistrations)
        {
            var existingSettingNames = registration.Settings
                .Select(setting => setting.Name)
                .ToHashSet(StringComparer.Ordinal);
            var hydratedSourceSettings = new HashSet<string>(StringComparer.Ordinal);

            foreach (var targetSetting in migrateFromTargets)
            {
                if (existingSettingNames.Contains(targetSetting.Name) || !targetSetting.IsSecret)
                    continue;

                var sourceSetting = registration.Settings.FirstOrDefault(setting => setting.Name == targetSetting.MigrateFrom);
                if (sourceSetting is not { IsSecret: true } || sourceSetting.Value?.GetValue() is not null)
                    continue;

                if (hydratedSourceSettings.Add(sourceSetting.Name))
                    await _secretStoreHandler.HydrateSecret(registration, sourceSetting.Name);
            }
        }
    }

    private void ApplyCustomMigrationResult(
        SettingBusinessEntity newSetting,
        SettingBusinessEntity matchingSetting,
        SettingMigrationResultDataContract migrationResult)
    {
        if (matchingSetting.IsSecret && !newSetting.IsSecret)
        {
            throw new InvalidOperationException(
                $"Custom MigrateFrom migration from secret setting '{matchingSetting.Name}' " +
                $"to non-secret setting '{newSetting.Name}' is not allowed.");
        }

        var currentFingerprint = ComputeMigrationFingerprint(matchingSetting);
        if (!string.Equals(currentFingerprint, migrationResult.SourceValueFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unable to apply custom MigrateFrom migration for setting '{newSetting.Name}' because " +
                $"the source setting '{matchingSetting.Name}' changed while registration was in progress.");
        }

        var migratedValue = _settingConverter.Convert(migrationResult.MigratedValue, newSetting);
        newSetting.Value = migratedValue;
        newSetting.LastChanged = matchingSetting.LastChanged;
        newSetting.Validate();
    }

    private static SettingMigrationResultDataContract? GetCustomMigrationResult(
        SettingClientBusinessEntity registration,
        SettingBusinessEntity matchingSetting,
        SettingBusinessEntity newSetting,
        List<SettingMigrationResultDataContract> migrationResults)
    {
        return migrationResults.FirstOrDefault(result =>
            result.SourceSettingName == matchingSetting.Name &&
            result.TargetSettingName == newSetting.Name &&
            result.Instance == registration.Instance);
    }

    private static void AddSecretChangeIfRequired(
        Dictionary<Guid, List<ChangedSetting>> secretChanges,
        SettingClientBusinessEntity registration,
        SettingBusinessEntity setting,
        SettingValueBaseBusinessEntity? originalValue)
    {
        if (!setting.IsSecret || setting.Value?.GetValue() is null)
            return;

        if (!secretChanges.TryGetValue(registration.Id, out var changes))
        {
            changes = [];
            secretChanges[registration.Id] = changes;
        }

        changes.Add(new ChangedSetting(
            setting.Name,
            originalValue,
            setting.Value,
            setting.IsSecret,
            setting.GetDataGridDefinition(),
            setting.IsExternallyManaged));
    }

    private static void AddRenamedHistoryMigrationIfRequired(
        List<RenamedSettingHistoryMigration> renamedHistoryMigrations,
        SettingClientBusinessEntity updatedSettingDefinitions,
        SettingClientBusinessEntity registration,
        SettingBusinessEntity matchingSetting,
        SettingBusinessEntity newSetting)
    {
        var sourceStillExistsInDefinitions = updatedSettingDefinitions.Settings
            .Any(s => s.Name == matchingSetting.Name);
        if (sourceStillExistsInDefinitions)
            return;

        renamedHistoryMigrations.Add(new RenamedSettingHistoryMigration(
            registration.Id,
            updatedSettingDefinitions.Name,
            registration.Instance,
            matchingSetting.Name,
            newSetting.Name,
            matchingSetting.Value,
            newSetting.Value,
            matchingSetting.IsSecret,
            newSetting.IsSecret));
    }

    private static string ComputeMigrationFingerprint(SettingBusinessEntity setting)
    {
        var payload = JsonConvert.SerializeObject(new
        {
            setting.Name,
            ValueType = setting.ValueType?.AssemblyQualifiedName,
            Value = setting.IsSecret ? null : setting.Value,
            LastChangedUtcTicks = setting.LastChanged?.ToUniversalTime().Ticks
        }, JsonSettings.FigDefault);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private async Task RecordInitialSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
        {
            var value = setting.Value is DataGridSettingBusinessEntity dataGridVal
                ? ChangedSetting.GetDataGridValue(dataGridVal, setting.GetDataGridDefinition())
                : setting.Value;
            await _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                ClientId = client.Id,
                ChangedAt = DateTime.UtcNow,
                SettingName = setting.Name,
                Value = value,
                ChangedBy = "REGISTRATION"
            });
        }
    }

    private async Task PersistRenamedSettingHistory(RenamedSettingHistoryMigration migration)
    {
        var renamedEntries = await _settingHistoryRepository.RenameSetting(
            migration.ClientId,
            migration.SourceSettingName,
            migration.TargetSettingName);

        string changeMessage;
        SettingValueBaseBusinessEntity? historyValue;

        if (migration.SourceIsSecret || migration.TargetIsSecret)
        {
            changeMessage =
                $"Setting renamed from '{migration.SourceSettingName}' to '{migration.TargetSettingName}'.";
            historyValue = new StringSettingBusinessEntity(SecretConstants.SecretPlaceholder);
        }
        else
        {
            var sourceValue = ConvertHistoryValueToString(migration.SourceSettingName, migration.SourceValue);
            var targetValue = ConvertHistoryValueToString(migration.TargetSettingName, migration.TargetValue);
            changeMessage =
                $"Setting renamed from '{migration.SourceSettingName}' to '{migration.TargetSettingName}'. " +
                $"Value migrated from '{sourceValue}' to '{targetValue}'.";
            historyValue = migration.TargetValue;
        }

        await _settingHistoryRepository.Add(new SettingValueBusinessEntity
        {
            ClientId = migration.ClientId,
            SettingName = migration.TargetSettingName,
            Value = historyValue,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = MigrateFromHistoryChangedBy,
            ChangeMessage = changeMessage
        });

        _logger.LogInformation(
            "Migrated setting history for client {ClientName} instance {Instance} from {SourceSettingName} to {TargetSettingName}; updated {RenamedEntries} historical rows",
            migration.ClientName.Sanitize(),
            migration.Instance,
            migration.SourceSettingName,
            migration.TargetSettingName,
            renamedEntries);
    }

    private string ConvertHistoryValueToString(string settingName, SettingValueBaseBusinessEntity? value)
    {
        return _settingConverter.Convert(new SettingValueBusinessEntity
        {
            SettingName = settingName,
            Value = value,
            ChangedBy = MigrateFromHistoryChangedBy
        }).Value;
    }

    private async Task RecordIdenticalRegistration(List<SettingClientBusinessEntity> existingRegistrations)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        foreach (var registration in existingRegistrations)
        {
            await _eventLogRepository.Add(_eventLogFactory.IdenticalRegistration(registration.Id, registration.Name));
            registration.LastRegistration = DateTime.UtcNow;
            await _settingClientRepository.UpdateClient(registration);
        }
    }

    private async Task HandleUpdatedRegistration(
        SettingClientBusinessEntity clientBusinessEntity,
        List<SettingClientBusinessEntity> existingRegistrations,
        List<SettingMigrationResultDataContract> migrationResults)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        _logger.LogInformation("Updated registration for client {ClientName}", clientBusinessEntity.Name);

        var secretChanges = new Dictionary<Guid, List<ChangedSetting>>();
        await HydrateSecretMigrateFromSources(clientBusinessEntity, existingRegistrations);
        var renamedHistoryMigrations = UpdateRegistrationsWithNewDefinitions(
            clientBusinessEntity,
            existingRegistrations,
            migrationResults,
            secretChanges);
        foreach (var updatedDefinition in existingRegistrations)
        {
            if (secretChanges.TryGetValue(updatedDefinition.Id, out var changes) && changes.Any())
                await _secretStoreHandler.SaveSecrets(updatedDefinition, changes);

            await _secretStoreHandler.ClearSecrets(updatedDefinition);
            updatedDefinition.LastRegistration = DateTime.UtcNow;
            await _settingClientRepository.UpdateClient(updatedDefinition);
            await _eventLogRepository.Add(
                _eventLogFactory.UpdatedRegistration(updatedDefinition.Id, updatedDefinition.Name));
        }

        foreach (var migration in renamedHistoryMigrations)
            await PersistRenamedSettingHistory(migration);

        await _settingGroupService.ValidateClientRegistrationGroups(
            clientBusinessEntity.Name,
            clientBusinessEntity.Settings.Select(setting => setting.Name));

        await _settingChangeRepository.RegisterChange();
        await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
            new CheckPointTrigger($"Updated Registration for client {clientBusinessEntity.Name}", AuthenticatedUser?.Username));
        await _webHookDisseminationService.UpdatedClientRegistration(clientBusinessEntity);
    }

    private async Task HandleInitialRegistration(SettingClientBusinessEntity clientBusinessEntity, CurrentRegistrationStatus registrationStatus, string clientSecret)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        _logger.LogInformation("Initial registration for client {ClientName}", clientBusinessEntity.Name);
        
        using (Activity? _ = ApiActivitySource.Instance.StartActivity("HashPassword"))
        {
            if (registrationStatus != CurrentRegistrationStatus.IsWithinChangePeriodAndMatchesPreviousSecret)
            {
                if (string.IsNullOrWhiteSpace(clientSecret))
                    throw new ArgumentException("Client secret cannot be null or empty.", nameof(clientSecret));
                
                clientBusinessEntity.ClientSecret = BCrypt.Net.BCrypt.EnhancedHashPassword(clientSecret);
            }
        }
        
        
        
        await _settingClientRepository.RegisterClient(clientBusinessEntity);
        await RecordInitialSettingValues(clientBusinessEntity);
        await _eventLogRepository.Add(
            _eventLogFactory.InitialRegistration(clientBusinessEntity.Id, clientBusinessEntity.Name));

        await _secretStoreHandler.SaveSecrets(clientBusinessEntity);
        
        await ApplyDeferredImport(clientBusinessEntity);

        var settingsWithGroups = clientBusinessEntity.Settings
            .Where(s => !string.IsNullOrWhiteSpace(s.Group))
            .Select(s => (s.Name, s.Group!, s.ValueType?.FullName ?? "System.String"))
            .ToList();

        if (settingsWithGroups.Any())
        {
            await _settingGroupService.HandleInitialRegistrationGroups(clientBusinessEntity.Name, settingsWithGroups);
        }

        await _settingChangeRepository.RegisterChange();
        await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
            new CheckPointTrigger($"Initial Registration for client {clientBusinessEntity.Name}", AuthenticatedUser?.Username));
        await _webHookDisseminationService.NewClientRegistration(clientBusinessEntity);
    }

    private async Task ApplyDeferredImport(SettingClientBusinessEntity client)
    {
        var deferredImportClients = await _deferredClientImportRepository.GetClients(client.Name);
        foreach (var deferredImport in deferredImportClients.OrderBy(a => a.ImportTime))
        {
            try
            {
                var clientToUpdate = await ResolveDeferredImportTargetClient(client.Name, deferredImport.Instance);
                if (clientToUpdate == null)
                {
                    _logger.LogWarning(
                        "Skipping deferred import for client {ClientName} instance {Instance} because the target client does not exist yet",
                        client.Name.Sanitize(),
                        deferredImport.Instance);
                    continue;
                }

                var result = _settingApplier.ApplySettings(clientToUpdate, deferredImport);
                await _settingClientRepository.UpdateClient(clientToUpdate);
                await _settingChangeRecorder.RecordSettingChanges(result.Changes, null, DateTime.UtcNow, clientToUpdate,
                    deferredImport.AuthenticatedUser);
                await _eventLogRepository.Add(
                    _eventLogFactory.DeferredImportApplied(clientToUpdate.Name, clientToUpdate.Instance));
                await _deferredClientImportRepository.DeleteClient(deferredImport.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to apply deferred import for client {ClientName} instance {Instance}; import will remain pending",
                    client.Name.Sanitize(),
                    deferredImport.Instance);
            }
        }
    }

    private async Task<SettingClientBusinessEntity?> ResolveDeferredImportTargetClient(string clientName, string? instance)
    {
        if (instance is not null)
        {
            return await _settingClientRepository.GetClient(clientName, instance)
                   ?? await _clientOverrideService.CreateClientOverride(clientName, instance, AuthenticatedUser);
        }

        return await _settingClientRepository.GetClient(clientName, null);
    }
    
    private async Task EnsureRevertScheduledAfterPartialApply(
        string clientName,
        string? instance,
        SettingValueUpdatesDataContract updatedSettings,
        SettingClientBusinessEntity client,
        List<SettingDataContract> valueUpdates,
        DateTime revertAtUtc)
    {
        var existingChanges = await _deferredChangeRepository.GetAllChanges();
        if (existingChanges.Any(c =>
                c.ClientName == clientName &&
                c.Instance == instance &&
                c.ExecuteAtUtc == revertAtUtc))
        {
            return;
        }

        var originalValues = new List<SettingDataContract>();
        foreach (var dataContract in valueUpdates)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == dataContract.Name);
            if (setting is null)
                continue;

            var history = await _settingHistoryRepository.GetAll(client.Id, dataContract.Name);
            if (history.Count == 0)
                continue;

            var revertSource = history.Count >= 2 ? history[1] : history[0];
            originalValues.Add(new SettingDataContract(
                revertSource.SettingName,
                _settingConverter.Convert(revertSource.Value, setting.HasSchema())));
        }

        if (!originalValues.Any())
            return;

        var original = new SettingValueUpdatesDataContract(originalValues, updatedSettings.ChangeMessage, null);
        await ScheduleChange(clientName, instance, original, revertAtUtc, true);
    }
    
    private async Task ScheduleChange(string clientName, string? instance,
        SettingValueUpdatesDataContract updatedSettings, DateTime executeAt, bool isRevert)
    {
        var deferredChange = new DeferredChangeBusinessEntity
        {
            ClientName = clientName,
            Instance = instance,
            ExecuteAtUtc = executeAt,
            RequestingUser = AuthenticatedUser?.Username,
            ChangeSet = updatedSettings
        };
        
        _logger.LogInformation("Scheduled change for client {ClientName} and instance {Instance}. IsRevert {IsRevert} ScheduledFor: {ExecuteAt}" , clientName, instance, isRevert, executeAt);
        await _deferredChangeRepository.Schedule(deferredChange);
        await _eventLogRepository.Add(_eventLogFactory.ChangesScheduled(clientName, instance, AuthenticatedUser?.Username, updatedSettings, executeAt, isRevert, false));
    }
    
    private List<SettingDataContract> FilterChangedOverrides(
        List<SettingDataContract> overrides, 
        SettingClientBusinessEntity? existingClient)
    {
        // If client doesn't exist yet, all overrides are new
        if (existingClient == null)
            return overrides;
        
        var changedOverrides = new List<SettingDataContract>();
        
        foreach (var overrideContract in overrides)
        {
            var existingSetting = existingClient.Settings.FirstOrDefault(s => s.Name == overrideContract.Name);
            
            // If setting doesn't exist or value has changed, include it
            if (existingSetting == null || HasValueChanged(existingSetting, overrideContract))
            {
                changedOverrides.Add(overrideContract);
            }
        }
        
        return changedOverrides;
    }
    
    private bool HasValueChanged(SettingBusinessEntity existingSetting, SettingDataContract newOverride)
    {
        // Convert the data contract to business entity and serialize it (same approach used in UpdateSettingValues)
        var businessEntity = _settingConverter.Convert(newOverride, existingSetting);
        businessEntity.Serialize();
        
        // Compare the serialized JSON values
        return businessEntity.ValueAsJson != JsonConvert.SerializeObject(existingSetting.Value, JsonSettings.FigDefault);
    }

    private sealed class SettingUpdateOptions
    {
        private SettingUpdateOptions(
            bool requireUserAccess,
            bool requireUserClassification,
            bool allowSchedule,
            bool createMissingClientOverride,
            bool requireKnownSettings,
            bool markChangedSettingsExternallyManaged,
            string? actor,
            bool useActorForNotifications)
        {
            RequireUserAccess = requireUserAccess;
            RequireUserClassification = requireUserClassification;
            AllowSchedule = allowSchedule;
            CreateMissingClientOverride = createMissingClientOverride;
            RequireKnownSettings = requireKnownSettings;
            MarkChangedSettingsExternallyManaged = markChangedSettingsExternallyManaged;
            Actor = actor;
            UseActorForNotifications = useActorForNotifications;
        }

        public static SettingUpdateOptions WebUser { get; } = new(
            requireUserAccess: true,
            requireUserClassification: true,
            allowSchedule: true,
            createMissingClientOverride: true,
            requireKnownSettings: false,
            markChangedSettingsExternallyManaged: false,
            actor: null,
            useActorForNotifications: false);

        public static SettingUpdateOptions ClientOverride { get; } = new(
            requireUserAccess: false,
            requireUserClassification: true,
            allowSchedule: true,
            createMissingClientOverride: true,
            requireKnownSettings: false,
            markChangedSettingsExternallyManaged: false,
            actor: "CLIENT OVERRIDE",
            useActorForNotifications: false);

        public static SettingUpdateOptions ClientSelfUpdate { get; } = new(
            requireUserAccess: false,
            requireUserClassification: false,
            allowSchedule: false,
            createMissingClientOverride: false,
            requireKnownSettings: true,
            markChangedSettingsExternallyManaged: true,
            actor: "CLIENT SELF UPDATE",
            useActorForNotifications: true);

        public bool RequireUserAccess { get; }

        public bool RequireUserClassification { get; }

        public bool AllowSchedule { get; }

        public bool CreateMissingClientOverride { get; }

        public bool RequireKnownSettings { get; }

        public bool MarkChangedSettingsExternallyManaged { get; }

        public string? Actor { get; }

        public bool UseActorForNotifications { get; }
    }

    private sealed class RenamedSettingHistoryMigration
    {
        public RenamedSettingHistoryMigration(
            Guid clientId,
            string clientName,
            string? instance,
            string sourceSettingName,
            string targetSettingName,
            SettingValueBaseBusinessEntity? sourceValue,
            SettingValueBaseBusinessEntity? targetValue,
            bool sourceIsSecret,
            bool targetIsSecret)
        {
            ClientId = clientId;
            ClientName = clientName;
            Instance = instance;
            SourceSettingName = sourceSettingName;
            TargetSettingName = targetSettingName;
            SourceValue = sourceValue;
            TargetValue = targetValue;
            SourceIsSecret = sourceIsSecret;
            TargetIsSecret = targetIsSecret;
        }

        public Guid ClientId { get; }

        public string ClientName { get; }

        public string? Instance { get; }

        public string SourceSettingName { get; }

        public string TargetSettingName { get; }

        public SettingValueBaseBusinessEntity? SourceValue { get; }

        public SettingValueBaseBusinessEntity? TargetValue { get; }

        public bool SourceIsSecret { get; }

        public bool TargetIsSecret { get; }
    }
}
