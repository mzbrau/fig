using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public interface ISettingsService
{
    IEnumerable<SettingsClientDefinitionDataContract> GetAllClients();
    
    IEnumerable<SettingDataContract> GetSettings(string clientName, string clientSecret, string? instance);
    
    string RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract clientDefinition);
    
    void DeleteClient(string clientName, string? instance);
    
    void UpdateSettingValues(string clientName, string? instance, IEnumerable<SettingDataContract> updatedSettings);
}