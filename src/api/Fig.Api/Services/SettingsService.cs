using Fig.Api.Converters;
using Fig.Api.Repositories;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISettingConverter _settingConverter;
    private readonly ISettingDefinitionConverter _settingDefinitionConverter;

    public SettingsService(ILogger<SettingsService> logger,
        ISettingsRepository settingsRepository,
        IAuditLogRepository auditLogRepository,
        ISettingConverter settingConverter,
        ISettingDefinitionConverter settingDefinitionConverter)
    {
        _logger = logger;
        _settingsRepository = settingsRepository;
        _auditLogRepository = auditLogRepository;
        _settingConverter = settingConverter;
        _settingDefinitionConverter = settingDefinitionConverter;
    }

    public void RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract settingsDefinition)
    {
        var existingRegistration =
            _settingsRepository.GetRegistration(settingsDefinition.Name);

        if (IsAlreadyRegisteredWithDifferentSecret())
        {
            throw new UnauthorizedAccessException(
                "Settings for that service have already been registered with a different secret.");
        }

        var settings = _settingDefinitionConverter.Convert(settingsDefinition);

        settings.ClientSecret = clientSecret;
        // TODO: Only update details, not values.
        _settingsRepository.RegisterSettings(settings);

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
        var settings = _settingsRepository.GetAllSettings();
        foreach (var setting in settings)
        {
            yield return _settingDefinitionConverter.Convert(setting);
        }
    }

    public IEnumerable<SettingDataContract> GetSettings(string clientName, string clientSecret, string? instance)
    {
        var existingRegistration = _settingsRepository.GetClient(clientName, instance);

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
        var client = _settingsRepository.GetClient(id, instance);

        if (client == null)
        {
            var nonOverrideClient = _settingsRepository.GetClient(id);

            if (nonOverrideClient == null)
            {
                return;
            }

            client = nonOverrideClient.CreateOverride(instance);
        }

        foreach (var updatedSetting in updatedSettings)
        {
            var setting = client.Settings.FirstOrDefault(a => a.Name == updatedSetting.Name);

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