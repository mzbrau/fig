using Fig.Api.Converters;
using Fig.Api.Datalayer.BusinessEntities;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public class SettingsService : ISettingsService
{
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<SettingsService> _logger;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingConverter _settingConverter;
    private readonly ISettingDefinitionConverter _settingDefinitionConverter;
    private readonly ISettingHistoryRepository _settingHistoryRepository;

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
            throw new UnauthorizedAccessException(
                "Settings for that service have already been registered with a different secret.");

        var clientBusinessEntity = _settingDefinitionConverter.Convert(client);

        if (clientBusinessEntity.Settings.Any(a => !a.Isvalid())) throw new InvalidSettingException();

        clientBusinessEntity.ClientSecret = clientSecret;

        if (!existingRegistrations.Any())
        {
            _settingClientRepository.RegisterClient(clientBusinessEntity);
            RecordSettingValues(clientBusinessEntity);
            _eventLogRepository.Add(
                _eventLogFactory.InitialRegistration(clientBusinessEntity.Id, clientBusinessEntity.Name));
        }
        else if (existingRegistrations.All(x => x.HasEquivalentDefinitionTo(clientBusinessEntity)))
        {
            foreach (var registration in existingRegistrations)
                _eventLogRepository.Add(_eventLogFactory.IdenticalRegistration(registration.Id, registration.Name));
        }
        else
        {
            UpdateRegistrationsWithNewDefinitions(clientBusinessEntity, existingRegistrations);
            foreach (var updatedDefinition in existingRegistrations)
            {
                _settingClientRepository.UpdateClient(updatedDefinition);
                _eventLogRepository.Add(
                    _eventLogFactory.UpdatedRegistration(updatedDefinition.Id, updatedDefinition.Name));
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
        foreach (var setting in settings) yield return _settingDefinitionConverter.Convert(setting);
    }

    public IEnumerable<SettingDataContract> GetSettings(string clientName, string clientSecret, string? instance)
    {
        var existingRegistration = _settingClientRepository.GetClient(clientName, instance);

        if (existingRegistration != null && existingRegistration.ClientSecret != clientSecret)
            throw new UnauthorizedAccessException();

        if (existingRegistration == null) throw new KeyNotFoundException();

        foreach (var setting in existingRegistration.Settings) yield return _settingConverter.Convert(setting);
    }

    public void DeleteClient(string clientName, string? instance)
    {
        var client = _settingClientRepository.GetClient(clientName, instance);
        if (client != null) _settingClientRepository.DeleteClient(client);
    }

    public void UpdateSettingValues(string clientName, string? instance,
        IEnumerable<SettingDataContract> updatedSettings)
    {
        var dirty = false;
        var client = _settingClientRepository.GetClient(clientName, instance);

        if (client == null)
        {
            var nonOverrideClient = _settingClientRepository.GetClient(clientName);

            if (nonOverrideClient == null) throw new InvalidClientException(clientName);

            client = nonOverrideClient.CreateOverride(instance);
            _settingClientRepository.RegisterClient(client);
            dirty = true;
        }

        var updatedSettingBusinessEntities = updatedSettings.Select(a => _settingConverter.Convert(a));
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
                    updatedSetting.Value));
                dirty = true;
            }
        }

        if (client.Settings.Any(a => !a.Isvalid())) throw new InvalidSettingException();

        if (dirty)
        {
            _settingClientRepository.UpdateClient(client);
            foreach (var eventLog in eventLogs)
                _eventLogRepository.Add(eventLog);
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
}