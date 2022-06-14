using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.SettingVerification;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public class SettingsService : AuthenticatedService, ISettingsService
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEncryptionService _encryptionService;
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
        IEncryptionService encryptionService,
        IConfigurationRepository configurationRepository,
        IValidValuesHandler validValuesHandler)
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
        _encryptionService = encryptionService;
        _configurationRepository = configurationRepository;
        _validValuesHandler = validValuesHandler;
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

        if (IsAlreadyRegisteredWithDifferentSecret())
            throw new UnauthorizedAccessException(
                "Settings for that service have already been registered with a different secret.");

        if (!configuration.AllowDynamicVerifications)
            client.DynamicVerifications = new List<SettingDynamicVerificationDefinitionDataContract>();

        foreach (var verification in client.DynamicVerifications)
            await _settingVerifier.Compile(_settingVerificationConverter.Convert(verification));

        var clientBusinessEntity = _settingDefinitionConverter.Convert(client);

        clientBusinessEntity.Settings.ToList().ForEach(a => a.Validate());

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

        bool IsAlreadyRegisteredWithDifferentSecret()
        {
            return existingRegistrations.Any() &&
                   !BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, existingRegistrations.First().ClientSecret);
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

        if (existingRegistration == null)
            throw new KeyNotFoundException();

        if (!BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, existingRegistration.ClientSecret))
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

    public void UpdateSettingValues(string clientName, string? instance,
        IEnumerable<SettingDataContract> updatedSettings)
    {
        var dirty = false;
        var client = _settingClientRepository.GetClient(clientName, instance);

        if (client == null)
        {
            client = CreateClientOverride(clientName, instance);
            dirty = true;
        }

        var updatedSettingBusinessEntities = updatedSettings.Select(dataContract =>
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
                var originalValue = setting.Value;
                setting.Value = _validValuesHandler.GetValue(updatedSetting.Value, setting.ValueType,
                    setting.ValidValues, setting.CommonEnumerationKey);
                changes.Add(new ChangedSetting(setting.Name, originalValue, setting.Value, setting.ValueType,
                    setting.IsSecret));
                dirty = true;
            }
        }

        client.Settings.ToList().ForEach(a => a.Validate());

        if (dirty)
        {
            client.LastSettingValueUpdate = DateTime.UtcNow;
            _settingClientRepository.UpdateClient(client);
            RecordSettingChanges(changes, client, instance);
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

        var result = await _settingVerifier.Verify(verification, client.Settings);

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

    private SettingClientBusinessEntity CreateClientOverride(string clientName, string? instance)
    {
        var nonOverrideClient = _settingClientRepository.GetClient(clientName);

        if (nonOverrideClient == null)
            throw new UnknownClientException(clientName);

        var client = nonOverrideClient.CreateOverride(instance);
        _settingClientRepository.RegisterClient(client);
        _eventLogRepository.Add(
            _eventLogFactory.InstanceOverrideCreated(client.Id, clientName, instance, AuthenticatedUser));
        return client;
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
            registration.Settings.Clear();
            var values = settingValues[registration.Instance ?? "Default"];
            foreach (var setting in updatedSettingDefinitions.Settings)
            {
                var newSetting = setting.Clone();
                var matchingSetting = values.FirstOrDefault(a => a.Name == newSetting.Name);
                if (matchingSetting != null) newSetting.Value = matchingSetting.Value;
                registration.Settings.Add(newSetting);
            }
        }
    }

    private void RecordInitialSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
        {
            var value = setting.ValueType.Is(FigPropertyType.DataGrid)
                ? ChangedSetting.GetDataGridValue(setting.Value)
                : setting.Value;
            _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                // TODO check if this is populated here
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
    }

    private void HandleInitialRegistration(SettingClientBusinessEntity clientBusinessEntity)
    {
        _settingClientRepository.RegisterClient(clientBusinessEntity);
        RecordInitialSettingValues(clientBusinessEntity);
        _eventLogRepository.Add(
            _eventLogFactory.InitialRegistration(clientBusinessEntity.Id, clientBusinessEntity.Name));
    }

    private void RecordSettingChanges(List<ChangedSetting> changes, SettingClientBusinessEntity client,
        string? instance)
    {
        foreach (var change in changes)
        {
            _eventLogRepository.Add(_eventLogFactory.SettingValueUpdate(client.Id,
                client.Name,
                instance,
                change.Name,
                change.OriginalValue,
                change.NewValue,
                AuthenticatedUser));

            _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                ClientId = client.Id,
                SettingName = change.Name,
                ValueType = change.ValueType,
                Value = change.NewValue,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = AuthenticatedUser.Username
            });
        }
    }
}