using Fig.Api.Converters;
using Fig.Api.Datalayer.BusinessEntities;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly ISettingConverter _settingConverter;
    private readonly ISettingDefinitionConverter _settingDefinitionConverter;
    private readonly IEventLogFactory _eventLogFactory;

    public SettingsService(ILogger<SettingsService> logger,
        ISettingClientRepository settingClientRepository,
        IEventLogRepository eventLogRepository,
        ISettingHistoryRepository settingHistoryRepository,
        ISettingConverter settingConverter,
        ISettingDefinitionConverter settingDefinitionConverter,
        IEventLogFactory eventLogFactory)
    {
        _logger = logger;
        _settingClientRepository = settingClientRepository;
        _eventLogRepository = eventLogRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _settingConverter = settingConverter;
        _settingDefinitionConverter = settingDefinitionConverter;
        _eventLogFactory = eventLogFactory;
    }

    public void RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract client)
    {
        var existingRegistrations = _settingClientRepository.GetAllInstancesOfClient(client.Name).ToList();

        if (IsAlreadyRegisteredWithDifferentSecret())
        {
            throw new UnauthorizedAccessException(
                "Settings for that service have already been registered with a different secret.");
        }

        var clientBusinessEntity = _settingDefinitionConverter.Convert(client);

        clientBusinessEntity.ClientSecret = clientSecret;
        
        if (!existingRegistrations.Any())
        {
            _settingClientRepository.RegisterClient(clientBusinessEntity);
            RecordSettingValues(clientBusinessEntity);
            _eventLogRepository.Add(_eventLogFactory.InitialRegistration(clientBusinessEntity.Id, clientBusinessEntity.Name));
        }
        else if (existingRegistrations.All(x => x.HasEquivalentDefinitionTo(clientBusinessEntity)))
        {
            foreach(var registration in existingRegistrations)
                _eventLogRepository.Add(_eventLogFactory.IdenticalRegistration(registration.Id, registration.Name));
        }
        else
        {
            var updatedDefinitions = GetNewDefinitionsWithOriginalValues(clientBusinessEntity, existingRegistrations);
            foreach (var updatedDefinition in updatedDefinitions)
            {
                _settingClientRepository.UpdateClient(updatedDefinition);
                _eventLogRepository.Add(_eventLogFactory.UpdatedRegistration(updatedDefinition.Id, updatedDefinition.Name));
            }
        }

        bool IsAlreadyRegisteredWithDifferentSecret()
        {
            return existingRegistrations.Any() &&
                   existingRegistrations.First().ClientSecret != clientSecret;
        }
    }

    public IEnumerable<SettingsClientDefinitionDataContract> GetAllClients()
    {
        var settings = _settingClientRepository.GetAllClients();
        foreach (var setting in settings)
        {
            yield return _settingDefinitionConverter.Convert(setting);
        }
    }

    public IEnumerable<SettingDataContract> GetSettings(string clientName, string clientSecret, string? instance)
    {
        var existingRegistration = _settingClientRepository.GetClient(clientName, instance);

        if (existingRegistration != null && existingRegistration.ClientSecret != clientSecret)
        {
            throw new UnauthorizedAccessException();
        }

        if (existingRegistration == null)
        {
            throw new KeyNotFoundException();
        }

        foreach (var setting in existingRegistration.Settings)
        {
            yield return _settingConverter.Convert(setting);
        }
    }

    public void DeleteClient(string clientName, string? instance)
    {
        var client = _settingClientRepository.GetClient(clientName, instance);
        if (client != null)
        {
            _settingClientRepository.DeleteClient(client);
        }
    }

    public void UpdateSettingValues(string id, string? instance,
        IEnumerable<SettingDataContract> updatedSettings)
    {
        var client = _settingClientRepository.GetClient(id, instance);

        if (client == null)
        {
            var nonOverrideClient = _settingClientRepository.GetClient(id);

            if (nonOverrideClient == null)
            {
                return;
            }

            client = nonOverrideClient.CreateOverride(instance);
        }

        var updatedSettingBusinessEntities = updatedSettings.Select(a => _settingConverter.Convert(a));
        
        foreach (var updatedSetting in updatedSettingBusinessEntities)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == updatedSetting.Name);
            
            if (setting != null && updatedSetting.ValueAsJson != setting.ValueAsJson)
            {
                var originalValue = setting.Value;
                setting.Value = updatedSetting.Value;
                RegisterSettingValueChanged(client.Id, 
                    client.Name,
                    instance,
                    setting.Name,
                    originalValue,
                    updatedSetting.Value);
            }
        }
    }
    
    private IEnumerable<SettingClientBusinessEntity> GetNewDefinitionsWithOriginalValues(
        SettingClientBusinessEntity clientBusinessEntity, 
        List<SettingClientBusinessEntity> existingRegistrations)
    {
        var firstRegistration = existingRegistrations.FirstOrDefault();
        if (firstRegistration == null)
        {
            yield return clientBusinessEntity;
            yield break;
        }
        
        foreach (var setting in clientBusinessEntity.Settings)
        {
            var existingSetting =
                firstRegistration.Settings.FirstOrDefault(a =>
                    a.Name == setting.Name && a.ValueType == setting.ValueType);

            setting.Value = existingSetting != null ? existingSetting.Value : setting.DefaultValue;
        }

        yield return clientBusinessEntity;
        foreach (var registrationInstance in existingRegistrations.Where(a => a.Instance != null))
        {
            yield return clientBusinessEntity.CreateOverride(registrationInstance.Instance);
        }
    }

    private void RecordSettingValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings)
        {
            _settingHistoryRepository.Add(new SettingValueBusinessEntity()
            {
                // TODO check if this is populated here
                ClientId = client.Id,
                ChangedAt = DateTime.UtcNow,
                SettingName = setting.Name,
                Value = setting.Value
            });
        }
    }

    private void RegisterSettingValueChanged(Guid clientId,
        string clientName,
        string? instance,
        string settingName,
        object originalValue,
        object newValue)
    {
        _eventLogRepository.Add(_eventLogFactory.SettingValueUpdate(clientId,
            clientName,
            instance,
            settingName,
            originalValue,
            newValue));
    }
}