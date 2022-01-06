using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISettingConverter _settingConverter;
    private readonly ISettingDefinitionConverter _settingDefinitionConverter;

    public SettingsService(ILogger<SettingsService> logger,
        ISettingClientRepository settingClientRepository,
        IAuditLogRepository auditLogRepository,
        ISettingConverter settingConverter,
        ISettingDefinitionConverter settingDefinitionConverter)
    {
        _logger = logger;
        _settingClientRepository = settingClientRepository;
        _auditLogRepository = auditLogRepository;
        _settingConverter = settingConverter;
        _settingDefinitionConverter = settingDefinitionConverter;
    }

    public void RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract settingsDefinition)
    {
        var existingRegistration =
            _settingClientRepository.GetClient(settingsDefinition.Name);

        if (IsAlreadyRegisteredWithDifferentSecret())
        {
            throw new UnauthorizedAccessException(
                "Settings for that service have already been registered with a different secret.");
        }

        var settings = _settingDefinitionConverter.Convert(settingsDefinition);

        settings.ClientSecret = clientSecret;

        if (existingRegistration != null)
        {
            // TODO: Only update details, not values.
            _settingClientRepository.UpdateClient(existingRegistration);
        }
        else
        {
            _settingClientRepository.RegisterClient(settings);
        }
        
        // TODO: Record the setting value;

        // TODO: Record the registration
        //var registrationDetails = 
        //_auditLogRepository.RecordRegistration()

        bool IsAlreadyRegisteredWithDifferentSecret()
        {
            return existingRegistration != null &&
                   existingRegistration.ClientSecret != clientSecret;
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
        throw new NotImplementedException();
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

        foreach (var updatedSetting in updatedSettings)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == updatedSetting.Name);

            // TODO: Dynamic objects cannot be directly compared. Need a better solution.
            if (setting != null && updatedSetting.Value != setting.Value)
            {
                var originalValue = setting.Value;
                setting.Value = updatedSetting.Value;
                RegisterSettingValueChanged(client.Name,
                    instance,
                    setting.Name,
                    originalValue,
                    updatedSetting.Value);
            }
        }
    }

    private void RegisterSettingValueChanged(string clientId,
        string? instance,
        string settingName,
        object originalValue,
        object newValue)
    {
        // TODO
        //_auditLogRepository.RecordSettingValueChanged()
    }
}