using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingChangeRepository
{
    SettingChangeBusinessEntity? GetLastChange();
    
    void RegisterChange();
}