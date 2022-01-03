using Fig.Api.Converters;
using Fig.Api.Repositories;
using Fig.Contracts.SettingConfiguration;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISettingQualifierConverter _settingQualifierConverter;
    private readonly ISettingConverter _settingConverter;
    private readonly ISettingDefinitionConverter _settingDefinitionConverter;
    private readonly ISettingConfigurationConverter _settingConfigurationConverter;

    public SettingsService(ILogger<SettingsService> logger,
        ISettingsRepository settingsRepository,
        IAuditLogRepository auditLogRepository,
        ISettingQualifierConverter settingQualifierConverter,
        ISettingConverter settingConverter,
        ISettingDefinitionConverter settingDefinitionConverter,
        ISettingConfigurationConverter settingConfigurationConverter)
    {
        _logger = logger;
        _settingsRepository = settingsRepository;
        _auditLogRepository = auditLogRepository;
        _settingQualifierConverter = settingQualifierConverter;
        _settingConverter = settingConverter;
        _settingDefinitionConverter = settingDefinitionConverter;
        _settingConfigurationConverter = settingConfigurationConverter;
    }

    public IEnumerable<SettingDataContract> GetSettings(SettingRequestDataContract request)
    {
        var qualifiers = _settingQualifierConverter.Convert(request.Qualifiers);

        if (!_settingsRepository.IsValidRequest(request.ClientName, request.ClientSecret))
        {
            throw new UnauthorizedAccessException();
        }
        
        var settings = _settingsRepository.GetSettings(request.ClientName, qualifiers);
        foreach (var setting in settings)
        {
            yield return _settingConverter.Convert(setting);
        }
    }

    public void UpdateSettingValues(SettingsClientDataContract updatedSettings)
    {
        var qualifiers = _settingQualifierConverter.Convert(updatedSettings.Qualifiers);
        var originalSettings = _settingsRepository.GetSettings(updatedSettings.Name, qualifiers).ToList();

        if (!originalSettings.Any())
        {
            return;
        }
        
        foreach (var updatedSetting in updatedSettings.Settings)
        {
            var setting = originalSettings.FirstOrDefault(a => a.Name == updatedSetting.Name);

            if (setting != null && updatedSetting.Value != setting.Value)
            {
                var originalValue = setting.Value;
                setting.Value = updatedSetting.Value;
                RegisterSettingValueChanged(updatedSettings.Qualifiers, setting.Name, originalValue,
                    updatedSetting.Value);
            }
        }
    }

    public void RegisterSettings(SettingsClientDefinitionDataContract settingsDefinition)
    {
        var existingRegistration =
            _settingsRepository.GetRegistration(settingsDefinition.Name);

        if (IsAlreadyRegisteredWithDifferentSecret())
        {
            throw new UnauthorizedAccessException(
                "Settings for that service have already been registered with a different secret.");
        }

        var settings = _settingDefinitionConverter.Convert(settingsDefinition);
        _settingsRepository.RegisterSettings(settings);
        
        // TODO: Record the registration
        //var registrationDetails = 
        //_auditLogRepository.RecordRegistration()

        bool IsAlreadyRegisteredWithDifferentSecret()
        {
            return existingRegistration != null && 
                   existingRegistration.ClientSecret != settingsDefinition.ClientSecret;
        }
    }

    public IEnumerable<SettingsClientConfigurationDataContract> GetSettingsForConfiguration()
    {
        var settings = _settingsRepository.GetAllSettings();
        foreach (var setting in settings)
        {
            yield return _settingConfigurationConverter.Convert(setting);
        }
    }
    
    private void RegisterSettingValueChanged(SettingQualifiersDataContract updatedSettingsQualifiers, string settingName, object originalValue, object updatedSettingValue)
    {
        // TODO
        //_auditLogRepository.RecordSettingValueChanged()
    }
}