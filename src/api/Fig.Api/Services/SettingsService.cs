using System.Diagnostics;
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
using Fig.Common.Events;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;
using NHibernate;

namespace Fig.Api.Services;

public class SettingsService : AuthenticatedService, ISettingsService
{
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

    public SettingsService(ILogger<SettingsService> logger,
        ISettingClientRepository settingClientRepository,
        IEventLogRepository eventLogRepository,
        ISettingHistoryRepository settingHistoryRepository,
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
        IDeferredChangeRepository deferredChangeRepository)
    {
        _logger = logger;
        _settingClientRepository = settingClientRepository;
        _eventLogRepository = eventLogRepository;
        _settingHistoryRepository = settingHistoryRepository;
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
    }

    public async Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract client)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var configuration = await _configurationRepository.GetConfiguration();
        if (!configuration.AllowNewRegistrations)
        {
            _logger.LogWarning("Registration of client {ClientName} blocked as registrations are disabled", client.Name.Sanitize());
            throw new UnauthorizedAccessException("New registrations are currently disabled");
        }

        var existingRegistrations = (await _settingClientRepository.GetAllInstancesOfClient(client.Name)).ToList();

        var registrationStatus = RegistrationStatusValidator.GetStatus(existingRegistrations, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
            throw new UnauthorizedAccessException(
                "Settings for that service have already been registered with a different secret.");

        var clientBusinessEntity = _settingDefinitionConverter.Convert(client);
        
        clientBusinessEntity.Settings.ToList().ForEach(a => a.Validate());

        using (Activity? _ = ApiActivitySource.Instance.StartActivity("HashPassword"))
        {
            if (registrationStatus != CurrentRegistrationStatus.IsWithinChangePeriodAndMatchesPreviousSecret)
                clientBusinessEntity.ClientSecret = BCrypt.Net.BCrypt.EnhancedHashPassword(clientSecret);
        }

        clientBusinessEntity.LastRegistration = DateTime.UtcNow;

        if (!existingRegistrations.Any())
        {
            await HandleInitialRegistration(clientBusinessEntity);
        }
        else if (existingRegistrations.All(x => x.HasEquivalentDefinitionTo(clientBusinessEntity)))
        {
            await RecordIdenticalRegistration(existingRegistrations);
        }
        else if (!configuration.AllowUpdatedRegistrations)
        {
            _logger.LogWarning(
                "Updated registration for client {ClientName} blocked as updated registrations are disabled", client.Name.Sanitize());
            throw new UnauthorizedAccessException("Updated registrations are currently disabled");
        }
        else
        {
            await HandleUpdatedRegistration(clientBusinessEntity, existingRegistrations);
        }
        
        if (ClientOverridesEnabledForClient() && 
            client.ClientSettingOverrides.Any())
        {
            _logger.LogInformation("Applying setting override for client {ClientName} with the following settings {SettingNames}",
                client.Name.Sanitize(), string.Join(", ", client.ClientSettingOverrides.Select(a => a.Name)));
            SetAuthenticatedUser(new ServiceUser());
            await UpdateSettingValues(
                client.Name, 
                client.Instance,
                new SettingValueUpdatesDataContract(client.ClientSettingOverrides, "Override from Client", null),
                true);
        }
        
        bool ClientOverridesEnabledForClient() =>
            configuration.AllowClientOverrides && 
            (string.IsNullOrEmpty(configuration.ClientOverridesRegex) ||
                Regex.IsMatch(client.Name, configuration.ClientOverridesRegex!));
    }

    public async Task<IEnumerable<SettingsClientDefinitionDataContract>> GetAllClients()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var allClients = await _settingClientRepository.GetAllClients(AuthenticatedUser, false);

        var configuration = await _configurationRepository.GetConfiguration();

        var clients = await Task.WhenAll(allClients.Select(async client =>
            await _settingDefinitionConverter.Convert(client, configuration.AllowDisplayScripts, AuthenticatedUser)));

        return clients.Where(a => a.Settings.Any());
    }

    public async Task<IEnumerable<SettingDataContract>> GetSettings(string clientName, string clientSecret, string? instance, Guid runSessionId)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingRegistration = await _settingClientRepository.GetClient(clientName, instance);

        if (existingRegistration == null && !string.IsNullOrEmpty(instance))
            existingRegistration = await _settingClientRepository.GetClient(clientName);
        
        if (existingRegistration == null)
            throw new KeyNotFoundException($"No existing registration for client '{clientName}'");

        var registrationStatus = RegistrationStatusValidator.GetStatus(existingRegistration, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
            throw new UnauthorizedAccessException();
        
        var session = existingRegistration.RunSessions.FirstOrDefault(a => a.RunSessionId == runSessionId);
        if (session is not null)
        {
            session.LastSettingLoadUtc = DateTime.UtcNow;
            await _settingClientRepository.UpdateClient(existingRegistration);
        }
        
        await _eventLogRepository.Add(_eventLogFactory.SettingsRead(existingRegistration.Id, clientName, instance));

        await _secretStoreHandler.HydrateSecrets(existingRegistration);

        return existingRegistration.Settings.Select(a => _settingConverter.Convert(a));
    }

    public async Task DeleteClient(string clientName, string? instance)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var client = await _settingClientRepository.GetClient(clientName, instance);
        if (client != null)
        {
            await _settingClientRepository.DeleteClient(client);
            await _eventLogRepository.Add(_eventLogFactory.ClientDeleted(client.Id, clientName, instance, AuthenticatedUser));
            await _settingChangeRepository.RegisterChange();
            await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
                new CheckPointTrigger($"Client {client.Name.Sanitize()} deleted", AuthenticatedUser?.Username));
        }
    }

    public async Task UpdateSettingValues(string clientName, string? instance,
        SettingValueUpdatesDataContract updatedSettings, bool clientOverride = false)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        if (!clientOverride)
            ThrowIfNoAccess(clientName);

        if (updatedSettings.Schedule?.ApplyAtUtc != null)
        {
            if (updatedSettings.Schedule.RevertAtUtc.HasValue && updatedSettings.Schedule.RevertAtUtc <= updatedSettings.Schedule.ApplyAtUtc)
                throw new InvalidOperationException("Revert time must be after apply time");
            
            await ScheduleChange(clientName, instance, updatedSettings, updatedSettings.Schedule.ApplyAtUtc.Value, false);
            return;
        }
        
        var dirty = false;
        var client = await _settingClientRepository.GetClient(clientName, instance);

        if (client == null)
        {
            client = await CreateClientOverride(clientName, instance);
            dirty = true;
        }
        
        var timeOfUpdate = DateTime.UtcNow;

        var updatedSettingBusinessEntities = updatedSettings.ValueUpdates.Select(dataContract =>
        {
            var originalSetting = client.Settings.FirstOrDefault(a => a.Name == dataContract.Name);
            var businessEntity = _settingConverter.Convert(dataContract, originalSetting);
            businessEntity.Serialize();
            return businessEntity;
        });

        bool restartRequired = false;
        var changes = new List<ChangedSetting>();
        var originalValues = new List<SettingDataContract>();
        foreach (var updatedSetting in updatedSettingBusinessEntities)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == updatedSetting.Name);

            if (setting != null && 
                updatedSetting.ValueAsJson != JsonConvert.SerializeObject(setting.Value, JsonSettings.FigDefault))
            {
                if (!AuthenticatedUser.HasPermissionForClassification(setting))
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
                changes.Add(new ChangedSetting(setting.Name, originalValue, setting.Value,
                    setting.IsSecret, dataGridDefinition, setting.IsExternallyManaged));
                dirty = true;

                if (!setting.SupportsLiveUpdate)
                    restartRequired = true;
            }
        }

        client.Settings.ToList().ForEach(a => a.Validate());

        if (dirty)
        {
            await _secretStoreHandler.SaveSecrets(client, changes);
            await _secretStoreHandler.ClearSecrets(client);
            client.LastSettingValueUpdate = timeOfUpdate;
            await _settingClientRepository.UpdateClient(client);
            var user = clientOverride ? "CLIENT OVERRIDE" : AuthenticatedUser?.Username;
            await _settingChangeRecorder.RecordSettingChanges(changes, updatedSettings.ChangeMessage, timeOfUpdate, client, user);
            await _webHookDisseminationService.SettingValueChanged(changes, client, AuthenticatedUser?.Username, updatedSettings.ChangeMessage);
            await _settingChangeRepository.RegisterChange();
            await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
                new CheckPointTrigger($"Settings updated for client {client.Name.Sanitize()}", AuthenticatedUser?.Username));

            if (originalValues.Any())
            {
                var original = new SettingValueUpdatesDataContract(originalValues, updatedSettings.ChangeMessage, null);
                await ScheduleChange(clientName, instance, original, updatedSettings.Schedule!.RevertAtUtc!.Value, true);
            }
            
            _logger.LogInformation("Updated settings for client {ClientName} with the following settings {SettingNames}",
                client.Name.Sanitize(), string.Join(", ", changes.Select(a => a.Name)));
        }
        
        if (restartRequired)
            await _statusService.MarkRestartRequired(clientName, instance);
    }

    public async Task<IEnumerable<SettingValueDataContract>> GetSettingHistory(string clientName, string settingName,
        string? instance)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var client = await _settingClientRepository.GetClient(clientName, instance);

        if (client == null)
            throw new KeyNotFoundException("Unknown client and instance combination");

        if (client.Settings.All(a => a.Name != settingName))
            throw new KeyNotFoundException($"Client {clientName} does not have setting {settingName}");
        
        var history = await _settingHistoryRepository.GetAll(client.Id, settingName);
        return history.Select(a => _settingConverter.Convert(a));
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
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var clientDescriptions = await _settingClientRepository.GetClientDescriptions(AuthenticatedUser);

        var clientDescriptionContracts = clientDescriptions.Select(client => 
            new ClientDescriptionDataContract(client.Name, client.Description))
            .ToList();

        return new ClientsDescriptionDataContract(clientDescriptionContracts);
    }

    private async Task<SettingClientBusinessEntity> CreateClientOverride(string clientName, string? instance)
    {
        var nonOverrideClient = await _settingClientRepository.GetClient(clientName);

        if (nonOverrideClient == null)
            throw new UnknownClientException(clientName);

        var client = nonOverrideClient.CreateOverride(instance);
        await _settingClientRepository.RegisterClient(client);
        await _eventLogRepository.Add(
            _eventLogFactory.InstanceOverrideCreated(client.Id, clientName, instance, AuthenticatedUser));

        await CloneSettingHistory(nonOverrideClient, client);
        return client;
    }

    private async Task CloneSettingHistory(SettingClientBusinessEntity originalClient, SettingClientBusinessEntity instanceClient)
    {
        foreach (var setting in originalClient.Settings)
        {
            var history = await _settingHistoryRepository.GetAll(originalClient.Id, setting.Name);
            foreach (var historyItem in history)
            {
                await _settingHistoryRepository.Add(historyItem.Clone(instanceClient.Id));
            }
        }
    }

    private void UpdateRegistrationsWithNewDefinitions(
        SettingClientBusinessEntity updatedSettingDefinitions,
        List<SettingClientBusinessEntity> existingRegistrations)
    {
        var settingValues = existingRegistrations.ToDictionary(
            a => a.Instance ?? "Default",
            b => b.Settings.ToList());

        foreach (var registration in existingRegistrations)
        {
            registration.Description = updatedSettingDefinitions.Description;
            registration.Settings.Clear();
            var values = settingValues[registration.Instance ?? "Default"];
            foreach (var setting in updatedSettingDefinitions.Settings)
            {
                var newSetting = setting.Clone();
                var matchingSetting = values.FirstOrDefault(a => a.Name == newSetting.Name);
                if (matchingSetting != null && matchingSetting.ValueType == newSetting.ValueType)
                {
                    newSetting.Value = matchingSetting.Value;
                    newSetting.LastChanged = matchingSetting.LastChanged;
                }
                registration.Settings.Add(newSetting);
            }
        }
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

    private async Task HandleUpdatedRegistration(SettingClientBusinessEntity clientBusinessEntity,
        List<SettingClientBusinessEntity> existingRegistrations)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        _logger.LogInformation("Updated registration for client {ClientName}", clientBusinessEntity.Name);

        UpdateRegistrationsWithNewDefinitions(clientBusinessEntity, existingRegistrations);
        foreach (var updatedDefinition in existingRegistrations)
        {
            await _secretStoreHandler.SaveSecrets(updatedDefinition);
            updatedDefinition.LastRegistration = DateTime.UtcNow;
            await _settingClientRepository.UpdateClient(updatedDefinition);
            await _eventLogRepository.Add(
                _eventLogFactory.UpdatedRegistration(updatedDefinition.Id, updatedDefinition.Name));
        }

        await _settingChangeRepository.RegisterChange();
        await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
            new CheckPointTrigger($"Updated Registration for client {clientBusinessEntity.Name}", AuthenticatedUser?.Username));
        await _webHookDisseminationService.UpdatedClientRegistration(clientBusinessEntity);
    }

    private async Task HandleInitialRegistration(SettingClientBusinessEntity clientBusinessEntity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        _logger.LogInformation("Initial registration for client {ClientName}", clientBusinessEntity.Name);
        await _settingClientRepository.RegisterClient(clientBusinessEntity);
        await RecordInitialSettingValues(clientBusinessEntity);
        await _eventLogRepository.Add(
            _eventLogFactory.InitialRegistration(clientBusinessEntity.Id, clientBusinessEntity.Name));

        await _secretStoreHandler.SaveSecrets(clientBusinessEntity);
        
        await ApplyDeferredImport(clientBusinessEntity);
        await _settingChangeRepository.RegisterChange();
        await _eventDistributor.PublishAsync(EventConstants.CheckPointTrigger,
            new CheckPointTrigger($"Initial Registration for client {clientBusinessEntity.Name}", AuthenticatedUser?.Username));
        await _webHookDisseminationService.NewClientRegistration(clientBusinessEntity);
    }

    private async Task ApplyDeferredImport(SettingClientBusinessEntity client)
    {
        var deferredImportClients = await _deferredClientImportRepository.GetClients(client.Name, client.Instance);
        foreach (var deferredImport in deferredImportClients.OrderBy(a => a.ImportTime))
        {
            var changes = _settingApplier.ApplySettings(client, deferredImport);
            await _settingClientRepository.UpdateClient(client);
            await _settingChangeRecorder.RecordSettingChanges(changes, null, DateTime.UtcNow, client, deferredImport.AuthenticatedUser);
            await _eventLogRepository.Add(_eventLogFactory.DeferredImportApplied(client.Name, client.Instance));
            await _deferredClientImportRepository.DeleteClient(deferredImport.Id);
        }
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
}