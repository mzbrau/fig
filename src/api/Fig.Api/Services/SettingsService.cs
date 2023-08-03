using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.SettingVerification;
using Fig.Api.Utils;
using Fig.Api.Validators;
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
    private readonly ISettingVerifier _settingVerifier;
    private readonly IValidatorApplier _validatorApplier;
    private readonly IValidValuesHandler _validValuesHandler;
    private readonly IDeferredClientImportRepository _deferredClientImportRepository;
    private readonly IDeferredSettingApplier _deferredSettingApplier;
    private readonly ISettingChangeRecorder _settingChangeRecorder;
    private readonly IWebHookDisseminationService _webHookDisseminationService;
    private readonly IVerificationHistoryRepository _verificationHistoryRepository;

    public SettingsService(ILogger<SettingsService> logger,
        ISettingClientRepository settingClientRepository,
        IEventLogRepository eventLogRepository,
        ISettingHistoryRepository settingHistoryRepository,
        IVerificationHistoryRepository verificationHistoryRepository,
        ISettingConverter settingConverter,
        ISettingDefinitionConverter settingDefinitionConverter,
        ISettingVerificationConverter settingVerificationConverter,
        ISettingVerifier settingVerifier,
        IEventLogFactory eventLogFactory,
        IValidatorApplier validatorApplier,
        IConfigurationRepository configurationRepository,
        IValidValuesHandler validValuesHandler,
        IDeferredClientImportRepository deferredClientImportRepository,
        IDeferredSettingApplier deferredSettingApplier,
        ISettingChangeRecorder settingChangeRecorder,
        IWebHookDisseminationService webHookDisseminationService)
    {
        _logger = logger;
        _settingClientRepository = settingClientRepository;
        _eventLogRepository = eventLogRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _verificationHistoryRepository = verificationHistoryRepository;
        _settingConverter = settingConverter;
        _settingDefinitionConverter = settingDefinitionConverter;
        _settingVerificationConverter = settingVerificationConverter;
        _settingVerifier = settingVerifier;
        _eventLogFactory = eventLogFactory;
        _validatorApplier = validatorApplier;
        _configurationRepository = configurationRepository;
        _validValuesHandler = validValuesHandler;
        _deferredClientImportRepository = deferredClientImportRepository;
        _deferredSettingApplier = deferredSettingApplier;
        _settingChangeRecorder = settingChangeRecorder;
        _webHookDisseminationService = webHookDisseminationService;
    }

    public async Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract client)
    {
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

        if (!configuration.AllowDynamicVerifications)
            client.DynamicVerifications = new List<SettingDynamicVerificationDefinitionDataContract>();

        foreach (var verification in client.DynamicVerifications)
            await _settingVerifier.Compile(_settingVerificationConverter.Convert(verification));

        var clientBusinessEntity = _settingDefinitionConverter.Convert(client);
        
        clientBusinessEntity.Settings.ToList().ForEach(a => a.Validate());

        if (registrationStatus != CurrentRegistrationStatus.IsWithinChangePeriodAndMatchesPreviousSecret)
            clientBusinessEntity.ClientSecret = BCrypt.Net.BCrypt.EnhancedHashPassword(clientSecret);
        
        clientBusinessEntity.LastRegistration = DateTime.UtcNow;

        if (!existingRegistrations.Any())
        {
            HandleInitialRegistration(clientBusinessEntity);
        }
        else if (existingRegistrations.All(x => x.HasEquivalentDefinitionTo(clientBusinessEntity)))
        {
            RecordIdenticalRegistration(existingRegistrations);
        }
        else if (!configuration.AllowUpdatedRegistrations)
        {
            _logger.LogInformation(
                $"Updated registration for client {client.Name} blocked as updated registrations are disabled.");
            throw new UnauthorizedAccessException("Updated registrations are currently disabled");
        }
        else
        {
            HandleUpdatedRegistration(clientBusinessEntity, existingRegistrations);
        }
    }

    public IEnumerable<SettingsClientDefinitionDataContract> GetAllClients()
    {
        var allClients = _settingClientRepository.GetAllClients();
        foreach (var client in allClients)
            yield return _settingDefinitionConverter.Convert(client);
    }

    public IEnumerable<SettingDataContract> GetSettings(string clientName, string clientSecret, string? instance)
    {
        var existingRegistration = _settingClientRepository.GetClient(clientName, instance);

        if (existingRegistration == null && !string.IsNullOrEmpty(instance))
            existingRegistration = _settingClientRepository.GetClient(clientName);
        
        if (existingRegistration == null)
            throw new KeyNotFoundException();

        var registrationStatus = RegistrationStatusValidator.GetStatus(existingRegistration, clientSecret);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
            throw new UnauthorizedAccessException();

        _eventLogRepository.Add(_eventLogFactory.SettingsRead(existingRegistration.Id, clientName, instance));
        _settingClientRepository.UpdateClient(existingRegistration);

        foreach (var setting in existingRegistration.Settings)
            yield return _settingConverter.Convert(setting);
    }

    public void DeleteClient(string clientName, string? instance)
    {
        var client = _settingClientRepository.GetClient(clientName, instance);
        if (client != null)
        {
            _settingClientRepository.DeleteClient(client);
            _eventLogRepository.Add(_eventLogFactory.ClientDeleted(client.Id, clientName, instance, AuthenticatedUser));
        }
    }

    public async Task UpdateSettingValues(string clientName, string? instance,
        SettingValueUpdatesDataContract updatedSettings)
    {
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
            var businessEntity = _settingConverter.Convert(dataContract);
            businessEntity.Serialize();
            return businessEntity;
        });

        var changes = new List<ChangedSetting>();
        foreach (var updatedSetting in updatedSettingBusinessEntities)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == updatedSetting.Name);

            if (setting != null && updatedSetting.ValueAsJson != setting.ValueAsJson)
            {
                var dataGridDefinition = setting.DataGridDefinitionJson is null
                    ? null
                    : JsonConvert.DeserializeObject<DataGridDefinitionDataContract>(setting.DataGridDefinitionJson);
                var originalValue = setting.Value;
                setting.Value = _validValuesHandler.GetValue(updatedSetting.Value,
                    setting.ValidValues, setting.ValueType, setting.LookupTableKey, dataGridDefinition);
                setting.LastChanged = timeOfUpdate;
                changes.Add(new ChangedSetting(setting.Name, originalValue, setting.Value,
                    setting.IsSecret));
                dirty = true;
            }
        }

        client.Settings.ToList().ForEach(a => a.Validate());

        if (dirty)
        {
            
            client.LastSettingValueUpdate = timeOfUpdate;
            _settingClientRepository.UpdateClient(client);
            _settingChangeRecorder.RecordSettingChanges(changes, updatedSettings.ChangeMessage, timeOfUpdate, client, AuthenticatedUser?.Username);
            await _webHookDisseminationService.SettingValueChanged(changes, client, AuthenticatedUser?.Username);
        }
    }

    public async Task<VerificationResultDataContract> RunVerification(string clientName, string verificationName,
        string? instance)
    {
        var client = _settingClientRepository.GetClient(clientName, instance);

        var verification = client?.GetVerification(verificationName);

        if (verification == null)
            throw new UnknownVerificationException(
                $"Client {clientName} does not exist or does not have a verification called {verificationName} defined.");

        if (verification is SettingDynamicVerificationBusinessEntity)
        {
            var configuration = _configurationRepository.GetConfiguration();
            if (!configuration.AllowDynamicVerifications)
                return VerificationResultDataContract.Failure("Dynamic verifications are disabled.");
        }

        var result = await _settingVerifier.Verify(verification, client!.Settings);

        _verificationHistoryRepository.Add(
            _settingVerificationConverter.Convert(result, client.Id, verificationName, AuthenticatedUser?.Username));
        _eventLogRepository.Add(_eventLogFactory.VerificationRun(client.Id, clientName, instance, verificationName,
            AuthenticatedUser, result.Success));
        return result;
    }

    public IEnumerable<SettingValueDataContract> GetSettingHistory(string clientName, string settingName,
        string? instance)
    {
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
        var client = _settingClientRepository.GetClient(clientName, instance);

        if (client == null)
            throw new KeyNotFoundException("Unknown client and instance combination");

        var history = _verificationHistoryRepository.GetAll(client.Id, verificationName);
        return history.Select(a => _settingVerificationConverter.Convert(a));
    }

    public ClientSecretChangeResponseDataContract ChangeSecret(string clientName,
        ClientSecretChangeRequestDataContract changeRequest)
    {
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

        return new ClientSecretChangeResponseDataContract(clientName, changeRequest.OldSecretExpiryUtc);
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
            var value = setting.Value is DataGridSettingBusinessEntity
                ? ChangedSetting.GetDataGridValue(setting.Value)
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
        foreach (var registration in existingRegistrations)
        {
            _eventLogRepository.Add(_eventLogFactory.IdenticalRegistration(registration.Id, registration.Name));
            registration.LastRegistration = DateTime.UtcNow;
            _settingClientRepository.UpdateClient(registration);
        }
    }

    private void HandleUpdatedRegistration(SettingClientBusinessEntity clientBusinessEntity,
        List<SettingClientBusinessEntity> existingRegistrations)
    {
        // TODO: Move these updates to a dedicated class.
        UpdateRegistrationsWithNewDefinitions(clientBusinessEntity, existingRegistrations);
        _validatorApplier.ApplyVerificationUpdates(existingRegistrations, clientBusinessEntity);
        foreach (var updatedDefinition in existingRegistrations)
        {
            updatedDefinition.LastRegistration = DateTime.UtcNow;
            _settingClientRepository.UpdateClient(updatedDefinition);
            _eventLogRepository.Add(
                _eventLogFactory.UpdatedRegistration(updatedDefinition.Id, updatedDefinition.Name));
        }

        _webHookDisseminationService.UpdatedClientRegistration(clientBusinessEntity);
    }

    private void HandleInitialRegistration(SettingClientBusinessEntity clientBusinessEntity)
    {
        _settingClientRepository.RegisterClient(clientBusinessEntity);
        RecordInitialSettingValues(clientBusinessEntity);
        _eventLogRepository.Add(
            _eventLogFactory.InitialRegistration(clientBusinessEntity.Id, clientBusinessEntity.Name));

        ApplyDeferredImport(clientBusinessEntity);
        _webHookDisseminationService.NewClientRegistration(clientBusinessEntity);
    }

    private void ApplyDeferredImport(SettingClientBusinessEntity client)
    {
        var deferredClientImport = _deferredClientImportRepository.GetClient(client.Name, client.Instance);
        if (deferredClientImport != null)
        {
            var changes = _deferredSettingApplier.ApplySettings(client, deferredClientImport);
            _settingClientRepository.UpdateClient(client);
            _settingChangeRecorder.RecordSettingChanges(changes, null, DateTime.UtcNow, client, deferredClientImport.AuthenticatedUser);
            _eventLogRepository.Add(_eventLogFactory.DeferredImportApplied(client.Name, client.Instance));
            _deferredClientImportRepository.DeleteClient(client.Name, client.Instance);
        }
    }
}