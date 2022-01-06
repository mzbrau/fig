using Fig.Api.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingsRepository
{
    SettingsClientBusinessEntity? GetRegistration(string name);
    void RegisterSettings(SettingsClientBusinessEntity settings);
    IEnumerable<SettingsClientBusinessEntity> GetAllSettings();
    SettingsClientBusinessEntity? GetClient(string name, string? instance = null);
}
