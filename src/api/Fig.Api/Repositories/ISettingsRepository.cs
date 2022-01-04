using Fig.Api.BusinessEntities;
using Fig.Contracts.Settings;

namespace Fig.Api.Repositories;

public interface ISettingsRepository
{
    SettingsClientBusinessEntity? GetRegistration(string name);
    string RegisterSettings(SettingsClientBusinessEntity settings);
    IEnumerable<SettingsClientBusinessEntity> GetAllSettings();
    SettingsClientBusinessEntity? GetClient(string id, string? hostname = null, string? username = null, string? instance = null);
}
