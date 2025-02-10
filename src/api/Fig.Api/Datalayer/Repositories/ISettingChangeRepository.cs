using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingChangeRepository
{
    Task<SettingChangeBusinessEntity?> GetLastChange();
    
    Task RegisterChange();
}