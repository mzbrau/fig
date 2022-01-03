using Fig.Contracts.SettingConfiguration;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public interface ISettingsService
{
    IEnumerable<SettingDataContract> GetSettings(SettingRequestDataContract request);
    
    void UpdateSettingValues(SettingsClientDataContract updatedSettings);
    
    void RegisterSettings(SettingsClientDefinitionDataContract settingsClientDefinition);
    
    IEnumerable<SettingsClientConfigurationDataContract> GetSettingsForConfiguration();
}