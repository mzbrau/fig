using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingMigrations;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public interface ISettingsService : IAuthenticatedService
{
    Task<SettingsClientLoadResult> GetAllClients();

    Task<IEnumerable<SettingDataContract>> GetSettings(string clientName, string clientSecret, string? instance, Guid runSessionId);

    Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract clientDefinition);

    Task<List<SettingMigrationRequestDataContract>> GetMigrateFromMigrationRequests(
        string clientSecret,
        SettingsClientDefinitionDataContract clientDefinition);

    Task DeleteClient(string clientName, string? instance);

    Task UpdateSettingValues(string clientName, string? instance, SettingValueUpdatesDataContract updatedSettings, bool clientOverride = false);

    Task UpdateSettingValuesFromClient(string clientName, string? instance, string clientSecret, SettingValueUpdatesDataContract updatedSettings);

    Task<IEnumerable<SettingValueDataContract>> GetSettingHistory(string clientName, string settingName, string? instance);

    Task<IEnumerable<ClientSettingsLastChangedDataContract>> GetLastChangedForAllClientsAndSettings();

    Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(string clientName, ClientSecretChangeRequestDataContract changeRequest);
    
    Task<DateTime> GetLastSettingUpdate();
    
    Task<ClientsDescriptionDataContract> GetClientDescriptions();
    
    Task UpdateClientDescription(string clientName, string? instance, string clientSecret, string description);

    void SetRequesterDetails(string? ipAddress, string? hostname);
}
