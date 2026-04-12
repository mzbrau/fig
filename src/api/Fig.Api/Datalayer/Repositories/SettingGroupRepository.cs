using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingGroupRepository : RepositoryBase<SettingGroupBusinessEntity>, ISettingGroupRepository
{
    public SettingGroupRepository(ISession session)
        : base(session)
    {
    }

    public async Task<IList<SettingGroupBusinessEntity>> GetAllGroups()
    {
        return await GetAll(false);
    }

    public async Task<SettingGroupBusinessEntity?> GetGroup(Guid id)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingGroupBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingGroupBusinessEntity.Id), id));
        criteria.SetLockMode(LockMode.Upgrade);
        return await criteria.UniqueResultAsync<SettingGroupBusinessEntity>();
    }

    public async Task<SettingGroupBusinessEntity?> GetGroupByName(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingGroupBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingGroupBusinessEntity.Name), name));
        return await criteria.UniqueResultAsync<SettingGroupBusinessEntity>();
    }

    public async Task<Guid> AddGroup(SettingGroupBusinessEntity group)
    {
        return await Save(group);
    }

    public async Task UpdateGroup(SettingGroupBusinessEntity group)
    {
        await Update(group);
    }

    public async Task DeleteGroup(SettingGroupBusinessEntity group)
    {
        await Delete(group);
    }
}
