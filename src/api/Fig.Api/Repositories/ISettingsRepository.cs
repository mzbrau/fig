using Fig.Api.BusinessEntities;
using Fig.Contracts.Settings;

namespace Fig.Api.Repositories;

public interface ISettingsRepository
{
    SettingsClientBusinessEntity? GetRegistration(string name);
    void RegisterSettings(SettingsClientBusinessEntity settings);
    IEnumerable<SettingsClientBusinessEntity> GetAllSettings();
    SettingsClientBusinessEntity? GetClient(string name, string? instance = null);
}
