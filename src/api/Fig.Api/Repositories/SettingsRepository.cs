using Fig.Api.BusinessEntities;

namespace Fig.Api.Repositories;

public class SettingsRepository : ISettingsRepository
{
    public IEnumerable<SettingBusinessEntity> GetSettings(string clientName, SettingQualifiersBusinessEntity qualifiers)
    {
        throw new NotImplementedException();
    }

    public SettingsClientBusinessEntity? GetRegistration(string name)
    {
        throw new NotImplementedException();
    }

    public void RegisterSettings(SettingsClientBusinessEntity settings)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<SettingsClientBusinessEntity> GetAllSettings()
    {
        throw new NotImplementedException();
    }

    public bool IsValidRequest(string requestClientName, string requestClientSecret)
    {
        throw new NotImplementedException();
    }
}