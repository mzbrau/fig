using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.Status;

namespace Fig.Api.Services;

public interface ISettingsService : IAuthenticatedService
{
    Task<IEnumerable<SettingsClientDefinitionDataContract>> GetAllClients();

    Task<IEnumerable<SettingDataContract>> GetSettings(string clientName, string clientSecret, string? instance, Guid runSessionId);

    Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract clientDefinition);

    Task DeleteClient(string clientName, string? instance);

    Task UpdateSettingValues(string clientName, string? instance, SettingValueUpdatesDataContract updatedSettings, bool clientOverride = false);

    Task<IEnumerable<SettingValueDataContract>> GetSettingHistory(string clientName, string settingName, string? instance);

    Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(string clientName, ClientSecretChangeRequestDataContract changeRequest);
    
    Task<DateTime> GetLastSettingUpdate();
    
    Task<ClientsDescriptionDataContract> GetClientDescriptions();
    
    void SetRequesterDetails(string? ipAddress, string? hostname);
}