using System.Diagnostics;
using System.Text.RegularExpressions;
using Fig.Api.Appliers;
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
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

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
    private readonly ISettingVerificationConverter _settingVerificationConverter;
    private readonly SettingVerification.ISettingVerification _settingPluginVerification;
    private readonly IVerificationApplier _verificationApplier;
    private readonly IValidValuesHandler _validValuesHandler;
    private readonly IDeferredClientImportRepository _deferredClientImportRepository;
    private readonly ISettingChangeRepository _settingChangeRepository;
    private readonly ISettingApplier _settingApplier;
    private readonly ISettingChangeRecorder _settingChangeRecorder;
    private readonly IWebHookDisseminationService _webHookDisseminationService;
    private readonly IStatusService _statusService;
    private readonly ISecretStoreHandler _secretStoreHandler;
    private readonly IEventDistributor _eventDistributor;
    private readonly IVerificationHistoryRepository _verificationHistoryRepository;

    public SettingsService(ILogger<SettingsService> logger,
        ISettingClientRepository settingClientRepository,
        IEventLogRepository eventLogRepository,
        ISettingHistoryRepository settingHistoryRepository,
        IVerificationHistoryRepository verificationHistoryRepository,
        ISettingConverter settingConverter,
        ISettingDefinitionConverter settingDefinitionConverter,
        ISettingVerificationConverter settingVerificationConverter,
        SettingVerification.ISettingVerification settingPluginVerification,
        IEventLogFactory eventLogFactory,
        IVerificationApplier verificationApplier,
        IConfigurationRepository configurationRepository,
        IValidValuesHandler validValuesHandler,
        IDeferredClientImportRepository deferredClientImportRepository,
        ISettingChangeRepository settingChangeRepository,
        ISettingApplier settingApplier,
        ISettingChangeRecorder settingChangeRecorder,
        IWebHookDisseminationService webHookDisseminationService,
        IStatusService statusService,
        ISecretStoreHandler secretStoreHandler,
        IEventDistributor eventDistributor)
    {
        _logger = logger;
        _settingClientRepository = settingClientRepository;
        _eventLogRepository = eventLogRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _verificationHistoryRepository = verificationHistoryRepository;
        _settingConverter = settingConverter;
        _settingDefinitionConverter = settingDefinitionConverter;
        _settingVerificationConverter = settingVerificationConverter;
        _settingPluginVerification = settingPluginVerification;
        _eventLogFactory = eventLogFactory;
        _verificationApplier = verificationApplier;
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
    }

    public async Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract client)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var configuration = _configurationRepository.GetConfiguration();
        if (!configuration.AllowNewRegistrations)
        {
            _logger.LogInformation($"Registration of client {client.Name} blocked as registrations are disabled.");
            throw new UnauthorizedAccessException("New registrations are currently disabled");
        }

        var existingRegistrations = _settingClientRepository.GetAllInstancesOfClient(client.Name).ToList();

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
            RecordIdenticalRegistration(existingRegistrations);
        }
        else if (!configuration.AllowUpdatedRegistrations)
        {
            _logger.LogInformation(
                "Updated registration for client {ClientName} blocked as updated registrations are disabled", client.Name);
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
                client.Name, string.Join(", ", client.ClientSettingOverrides.Select(a => a.Name)));
            await UpdateSettingValues(
                client.Name, 
                client.Instance,
                new SettingValueUpdatesDataContract(client.ClientSettingOverrides, "Override from Client"),
                true);
        }
        
        bool ClientOverridesEnabledForClient() =>
        configuration?.AllowClientOverrides == true && 
        (string.IsNullOrEmpty(configuration.ClientOverridesRegex) ||
         Regex.IsMatch(client.Name, configuration.ClientOverridesRegex!));
    }

    public IEnumerable<SettingsClientDefinitionDataContract> GetAllClients()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var allClients = _settingClientRepository.GetAllClients(AuthenticatedUser, false);
        
        var configuration = _configurationRepository.GetConfiguration();
        
        foreach (var client in allClients)
            yield return _settingDefinitionConverter.Convert(client, configuration.AllowDisplayScripts);
    }

    public IEnumerable<SettingDataContract> GetSettings(string clientName, string clientSecret, string? instance, Guid runSessionId)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingRegistration = _settingClientRepository.GetClient(clientName, instance);

        if (existingRegistration == null && !string.IsNullOrEmpty(instance))
            existingRegistration = _settingClientRepository.GetClient(clientName);
        
        if (existingRegistration == null)
            throw new KeyNotFoundException($"No existing registration for client '{clientName}'");

        var registrationStatus = RegistrationStatusValidator.GetStatus(existingRegistration, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
            throw new UnauthorizedAccessException();
        
        var session = existingRegistration.RunSessions.FirstOrDefault(a => a.RunSessionId == runSessionId);
        if (session is not null)
        {
            session.LastSettingLoadUtc = DateTime.UtcNow;
            _settingClientRepository.UpdateClient(existingRegistration);
        }
        
        _eventLogRepository.Add(_eventLogFactory.SettingsRead(existingRegistration.Id, clientName, instance));

        _secretStoreHandler.HydrateSecrets(existingRegistration);

        foreach (var setting in existingRegistration.Settings)
            yield return _settingConverter.Convert(setting);
    }

    public void DeleteClient(string clientName, string? instance)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var client = _settingClientRepository.GetClient(clientName, instance);
        if (client != null)
        {
            _settingClientRepository.DeleteClient(client);
            _eventLogRepository.Add(_eventLogFactory.ClientDeleted(client.Id, clientName, instance, AuthenticatedUser));
            _settingChangeRepository.RegisterChange();
            _eventDistributor.Publish(EventConstants.CheckPointRequired,
                new CheckPointRecord($"Client {client.Name} deleted", AuthenticatedUser.Username));
        }
    }

    public async Task UpdateSettingValues(string clientName, string? instance,
        SettingValueUpdatesDataContract updatedSettings, bool clientOverride = false)
    {
        if (!clientOverride)
            ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var dirty = false;
        var client = _settingClientRepository.GetClient(clientName, instance);

        if (client == null)
        {
            client = CreateClientOverride(clientName, instance);
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
        foreach (var updatedSetting in updatedSettingBusinessEntities)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == updatedSetting.Name);

            if (setting != null && 
                updatedSetting.ValueAsJson != JsonConvert.SerializeObject(setting.Value, JsonSettings.FigDefault))
            {
                var dataGridDefinition = setting.GetDataGridDefinition();
                var originalValue = setting.Value;
                setting.Value = _validValuesHandler.GetValue(updatedSetting.Value,
                    setting.ValidValues, setting.ValueType, setting.LookupTableKey, dataGridDefinition);
                setting.LastChanged = timeOfUpdate;
                changes.Add(new ChangedSetting(setting.Name, originalValue, setting.Value,
                    setting.IsSecret, dataGridDefinition));
                dirty = true;

                if (!setting.SupportsLiveUpdate)
                    restartRequired = true;
            }
        }

        client.Settings.ToList().ForEach(a => a.Validate());

        if (dirty)
        {
            await _secretStoreHandler.SaveSecrets(client, changes);
            _secretStoreHandler.ClearSecrets(client);
            client.LastSettingValueUpdate = timeOfUpdate;
            _settingClientRepository.UpdateClient(client);
            var user = clientOverride ? "CLIENT OVERRIDE" : AuthenticatedUser?.Username;
            _settingChangeRecorder.RecordSettingChanges(changes, updatedSettings.ChangeMessage, timeOfUpdate, client, user);
            await _webHookDisseminationService.SettingValueChanged(changes, client, AuthenticatedUser?.Username, updatedSettings.ChangeMessage);
            _settingChangeRepository.RegisterChange();
            _eventDistributor.Publish(EventConstants.CheckPointRequired,
                new CheckPointRecord($"Settings updated for client {client.Name}", AuthenticatedUser?.Username));
        }
        
        if (restartRequired)
            _statusService.MarkRestartRequired(clientName, instance);
    }

    public async Task<VerificationResultDataContract> RunVerification(string clientName, string verificationName,
        string? instance)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var client = _settingClientRepository.GetClient(clientName, instance);

        var verification = client?.GetVerification(verificationName);

        if (verification == null)
            throw new UnknownVerificationException(
                $"Client {clientName} does not exist or does not have a verification called {verificationName} defined.");

        var result = await _settingPluginVerification.RunVerification(verification, client!.Settings);

        _verificationHistoryRepository.Add(
            _settingVerificationConverter.Convert(result, client.Id, verificationName, AuthenticatedUser?.Username));
        _eventLogRepository.Add(_eventLogFactory.VerificationRun(client.Id, clientName, instance, verificationName,
            AuthenticatedUser, result.Success));
        return result;
    }

    public IEnumerable<SettingValueDataContract> GetSettingHistory(string clientName, string settingName,
        string? instance)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var client = _settingClientRepository.GetClient(clientName, instance);

        if (client == null)
            throw new KeyNotFoundException("Unknown client and instance combination");

        if (client.Settings.All(a => a.Name != settingName))
            throw new KeyNotFoundException($"Client {clientName} does not have setting {settingName}");
        
        var history = _settingHistoryRepository.GetAll(client.Id, settingName);
        return history.Select(a => _settingConverter.Convert(a));
    }

    public IEnumerable<VerificationResultDataContract> GetVerificationHistory(string clientName,
        string verificationName, string? instance)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var client = _settingClientRepository.GetClient(clientName, instance);

        if (client == null)
            throw new KeyNotFoundException("Unknown client and instance combination");

        var history = _verificationHistoryRepository.GetAll(client.Id, verificationName);
        return history.Select(a => _settingVerificationConverter.Convert(a));
    }

    public ClientSecretChangeResponseDataContract ChangeClientSecret(string clientName,
        ClientSecretChangeRequestDataContract changeRequest)
    {
        ThrowIfNoAccess(clientName);
        
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        var clients = _settingClientRepository.GetAllInstancesOfClient(clientName).ToList();

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
            _settingClientRepository.UpdateClient(client);
            _eventLogRepository.Add(_eventLogFactory.ClientSecretChanged(client.Id,
                client.Name,
                client.Instance,
                AuthenticatedUser,
                changeRequest.OldSecretExpiryUtc));
        }
        
        _eventDistributor.Publish(EventConstants.CheckPointRequired,
            new CheckPointRecord($"Secret changed for client {clientName}", AuthenticatedUser?.Username));

        return new ClientSecretChangeResponseDataContract(clientName, changeRequest.OldSecretExpiryUtc);
    }

    public DateTime GetLastSettingUpdate()
    {
        return _settingChangeRepository.GetLastChange()?.LastChange ?? DateTime.MinValue;
    }

    private SettingClientBusinessEntity CreateClientOverride(string clientName, string? instance)
    {
        var nonOverrideClient = _settingClientRepository.GetClient(clientName);

        if (nonOverrideClient == null)
            throw new UnknownClientException(clientName);

        var client = nonOverrideClient.CreateOverride(instance);
        _settingClientRepository.RegisterClient(client);
        _eventLogRepository.Add(
            _eventLogFactory.InstanceOverrideCreated(client.Id, clientName, instance, AuthenticatedUser));

        CloneSettingHistory(nonOverrideClient, client);
        return client;
    }

    private void CloneSettingHistory(SettingClientBusinessEntity originalClient, SettingClientBusinessEntity instanceClient)
    {
        foreach (var setting in originalClient.Settings)
        {
            var history = _settingHistoryRepository.GetAll(originalClient.Id, setting.Name);
            foreach (var historyItem in history)
            {
                _settingHistoryRepository.Add(historyItem.Clone(instanceClient.Id));
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
                if (matchingSetting != null)
                {
                    newSetting.Value = matchingSetting.Value;
                    newSetting.LastChanged = matchingSetting.LastChanged;
                }
                registration.Settings.Add(newSetting);
            }
        }
    }

    private void RecordInitialSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
        {
            var value = setting.Value is DataGridSettingBusinessEntity dataGridVal
                ? ChangedSetting.GetDataGridValue(dataGridVal, setting.GetDataGridDefinition())
                : setting.Value;
            _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                ClientId = client.Id,
                ChangedAt = DateTime.UtcNow,
                SettingName = setting.Name,
                Value = value,
                ChangedBy = "REGISTRATION"
            });
        }
    }

    private void RecordIdenticalRegistration(List<SettingClientBusinessEntity> existingRegistrations)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        foreach (var registration in existingRegistrations)
        {
            _eventLogRepository.Add(_eventLogFactory.IdenticalRegistration(registration.Id, registration.Name));
            registration.LastRegistration = DateTime.UtcNow;
            _settingClientRepository.UpdateClient(registration);
        }
    }

    private async Task HandleUpdatedRegistration(SettingClientBusinessEntity clientBusinessEntity,
        List<SettingClientBusinessEntity> existingRegistrations)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        _logger.LogInformation("Updated registration for client {ClientName}", clientBusinessEntity.Name);
        // TODO: Move these updates to a dedicated class.
        UpdateRegistrationsWithNewDefinitions(clientBusinessEntity, existingRegistrations);
        _verificationApplier.ApplyVerificationUpdates(existingRegistrations, clientBusinessEntity);
        foreach (var updatedDefinition in existingRegistrations)
        {
            await _secretStoreHandler.SaveSecrets(updatedDefinition);
            updatedDefinition.LastRegistration = DateTime.UtcNow;
            _settingClientRepository.UpdateClient(updatedDefinition);
            _eventLogRepository.Add(
                _eventLogFactory.UpdatedRegistration(updatedDefinition.Id, updatedDefinition.Name));
        }

        _settingChangeRepository.RegisterChange();
        _eventDistributor.Publish(EventConstants.CheckPointRequired,
            new CheckPointRecord($"Updated Registration for client {clientBusinessEntity.Name}", AuthenticatedUser?.Username));
        await _webHookDisseminationService.UpdatedClientRegistration(clientBusinessEntity);
    }

    private async Task HandleInitialRegistration(SettingClientBusinessEntity clientBusinessEntity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        _logger.LogInformation("Initial registration for client {ClientName}", clientBusinessEntity.Name);
        _settingClientRepository.RegisterClient(clientBusinessEntity);
        RecordInitialSettingValues(clientBusinessEntity);
        _eventLogRepository.Add(
            _eventLogFactory.InitialRegistration(clientBusinessEntity.Id, clientBusinessEntity.Name));

        await _secretStoreHandler.SaveSecrets(clientBusinessEntity);
        
        ApplyDeferredImport(clientBusinessEntity);
        _settingChangeRepository.RegisterChange();
        _eventDistributor.Publish(EventConstants.CheckPointRequired,
            new CheckPointRecord($"Initial Registration for client {clientBusinessEntity.Name}", AuthenticatedUser?.Username));
        await _webHookDisseminationService.NewClientRegistration(clientBusinessEntity);
    }

    private void ApplyDeferredImport(SettingClientBusinessEntity client)
    {
        var deferredImportClients = _deferredClientImportRepository.GetClients(client.Name, client.Instance);
        foreach (var deferredImport in deferredImportClients.OrderBy(a => a.ImportTime))
        {
            var changes = _settingApplier.ApplySettings(client, deferredImport);
            _settingClientRepository.UpdateClient(client);
            _settingChangeRecorder.RecordSettingChanges(changes, null, DateTime.UtcNow, client, deferredImport.AuthenticatedUser);
            _eventLogRepository.Add(_eventLogFactory.DeferredImportApplied(client.Name, client.Instance));
            _deferredClientImportRepository.DeleteClient(deferredImport.Id);
        }
    }
}