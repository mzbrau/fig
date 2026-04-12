using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingGroupRepository
{
    Task<IList<SettingGroupBusinessEntity>> GetAllGroups();

    Task<SettingGroupBusinessEntity?> GetGroup(Guid id);

    Task<SettingGroupBusinessEntity?> GetGroupByName(string name);

    Task<Guid> AddGroup(SettingGroupBusinessEntity group);

    Task UpdateGroup(SettingGroupBusinessEntity group);

    Task DeleteGroup(SettingGroupBusinessEntity group);
}
