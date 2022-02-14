using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.SettingVerification;
using Fig.Api.Validators;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public class SettingsService : AuthenticatedService, ISettingsService
{
    private readonly IValidatorApplier _validatorApplier;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<SettingsService> _logger;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingConverter _settingConverter;
    private readonly ISettingDefinitionConverter _settingDefinitionConverter;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly ISettingVerificationConverter _settingVerificationConverter;
    private readonly ISettingVerifier _settingVerifier;
    private string? _requesterHostname;
    private string? _requestIpAddress;

    public SettingsService(ILogger<SettingsService> logger,
        ISettingClientRepository settingClientRepository,
        IEventLogRepository eventLogRepository,
        ISettingHistoryRepository settingHistoryRepository,
        ISettingConverter settingConverter,
        ISettingDefinitionConverter settingDefinitionConverter,
        ISettingVerificationConverter settingVerificationConverter,
        ISettingVerifier settingVerifier,
        IEventLogFactory eventLogFactory,
        IValidatorApplier validatorApplier)
    {
        _logger = logger;
        _settingClientRepository = settingClientRepository;
        _eventLogRepository = eventLogRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _settingConverter = settingConverter;
        _settingDefinitionConverter = settingDefinitionConverter;
        _settingVerificationConverter = settingVerificationConverter;
        _settingVerifier = settingVerifier;
        _eventLogFactory = eventLogFactory;
        _validatorApplier = validatorApplier;
    }

    public async Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract client)
    {
        var existingRegistrations = _settingClientRepository.GetAllInstancesOfClient(client.Name).ToList();

        if (IsAlreadyRegisteredWithDifferentSecret())
            throw new UnauthorizedAccessException(
                "Settings for that service have already been registered with a different secret.");

        foreach (var verification in client.DynamicVerifications)
            await _settingVerifier.Compile(_settingVerificationConverter.Convert(verification));

        var clientBusinessEntity = _settingDefinitionConverter.Convert(client);

        clientBusinessEntity.Settings.ToList().ForEach(a => a.Validate());

        clientBusinessEntity.ClientSecret = BCrypt.Net.BCrypt.EnhancedHashPassword(clientSecret);
        clientBusinessEntity.Hostname = _requesterHostname;
        clientBusinessEntity.IpAddress = _requestIpAddress;
        clientBusinessEntity.LastRegistration = DateTime.UtcNow;

        if (!existingRegistrations.Any())
            HandleInitialRegistration(clientBusinessEntity);
        else if (existingRegistrations.All(x => x.HasEquivalentDefinitionTo(clientBusinessEntity)))
            RecordIdenticalRegistration(existingRegistrations);
        else
            HandleUpdatedRegistration(clientBusinessEntity, existingRegistrations);

        bool IsAlreadyRegisteredWithDifferentSecret()
        {
            return existingRegistrations.Any() &&
                   !BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, existingRegistrations.First().ClientSecret);
        }
    }

    public IEnumerable<SettingsClientDefinitionDataContract> GetAllClients()
    {
        var settings = _settingClientRepository.GetAllClients();
        foreach (var setting in settings)
            yield return _settingDefinitionConverter.Convert(setting);
    }

    public IEnumerable<SettingDataContract> GetSettings(string clientName, string clientSecret, string? instance)
    {
        var existingRegistration = _settingClientRepository.GetClient(clientName, instance);

        if (existingRegistration == null)
            throw new KeyNotFoundException();

        if (!BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, existingRegistration.ClientSecret))
            throw new UnauthorizedAccessException();

        _eventLogRepository.Add(_eventLogFactory.SettingsRead(existingRegistration.Id, clientName, instance));

        existingRegistration.Hostname = _requesterHostname;
        existingRegistration.IpAddress = _requestIpAddress;
        existingRegistration.LastRead = DateTime.UtcNow;
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
        var eventLogs = new List<EventLogBusinessEntity>();
        foreach (var updatedSetting in updatedSettingBusinessEntities)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == updatedSetting.Name);

            if (setting != null && updatedSetting.ValueAsJson != setting.ValueAsJson)
            {
                var originalValue = setting.Value;
                setting.Value = updatedSetting.Value;
                eventLogs.Add(_eventLogFactory.SettingValueUpdate(client.Id,
                    client.Name,
                    instance,
                    setting.Name,
                    originalValue,
                    updatedSetting.Value,
                    AuthenticatedUser));
                dirty = true;
            }
        }

        client.Settings.ToList().ForEach(a => a.Validate());

        if (dirty)
        {
            _settingClientRepository.UpdateClient(client);
            foreach (var eventLog in eventLogs)
                _eventLogRepository.Add(eventLog);
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

        var result = await _settingVerifier.Verify(verification, client.Settings);

        _eventLogRepository.Add(_eventLogFactory.VerificationRun(client.Id, clientName, instance, verificationName,
            AuthenticatedUser, result.Success));
        return result;
    }

    public void SetRequesterDetails(string? ipAddress, string? hostname)
    {
        _requestIpAddress = ipAddress;
        _requesterHostname = hostname;
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

    private void RecordSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
            _settingHistoryRepository.Add(new SettingValueBusinessEntity
            {
                // TODO check if this is populated here
                ClientId = client.Id,
                ChangedAt = DateTime.UtcNow,
                SettingName = setting.Name,
                Value = setting.Value
            });
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
        RecordSettingValues(clientBusinessEntity);
        _eventLogRepository.Add(
            _eventLogFactory.InitialRegistration(clientBusinessEntity.Id, clientBusinessEntity.Name));
    }
}