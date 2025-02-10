using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;

namespace Fig.Api.Services;

public interface ISettingsService : IAuthenticatedService
{
    Task<IEnumerable<SettingsClientDefinitionDataContract>> GetAllClients();

    Task<IEnumerable<SettingDataContract>> GetSettings(string clientName, string clientSecret, string? instance, Guid runSessionId);

    Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract clientDefinition);

    Task DeleteClient(string clientName, string? instance);

    Task UpdateSettingValues(string clientName, string? instance, SettingValueUpdatesDataContract updatedSettings, bool clientOverride = false);

    Task<VerificationResultDataContract> RunVerification(string clientName, string verificationName, string? instance);

    Task<IEnumerable<SettingValueDataContract>> GetSettingHistory(string clientName, string settingName, string? instance);

    Task<IEnumerable<VerificationResultDataContract>> GetVerificationHistory(string clientName, string verificationName,
        string? instance);

    Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(string clientName, ClientSecretChangeRequestDataContract changeRequest);
    
    Task<DateTime> GetLastSettingUpdate();
}