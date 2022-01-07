using Fig.Api.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingHistoryRepository
{
    void Add(SettingValueBusinessEntity settingValue);

    IEnumerable<SettingValueBusinessEntity> GetAll(Guid clientId, string settingName);
}