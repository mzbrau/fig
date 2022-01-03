using Fig.Api.BusinessEntities;
using Fig.Contracts.Settings;

namespace Fig.Api.Repositories;

public interface ISettingsRepository
{
    IEnumerable<SettingBusinessEntity> GetSettings(string clientName, SettingQualifiersBusinessEntity qualifiers);
    SettingsClientBusinessEntity? GetRegistration(string name);
    void RegisterSettings(SettingsClientBusinessEntity settings);
    IEnumerable<SettingsClientBusinessEntity> GetAllSettings();
    
    bool IsValidRequest(string requestClientName, string requestClientSecret);
}
