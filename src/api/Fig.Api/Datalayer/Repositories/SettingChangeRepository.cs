using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingChangeRepository : RepositoryBase<SettingChangeBusinessEntity>, ISettingChangeRepository
{
    public SettingChangeRepository(ISession session) 
        : base(session)
    {
    }
    
    public async Task<SettingChangeBusinessEntity?> GetLastChange()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingChangeBusinessEntity>();
        criteria.SetLockMode(LockMode.Upgrade);
        var result = await criteria.UniqueResultAsync<SettingChangeBusinessEntity>();
        return result;
    }

    public async Task RegisterChange()
    {
        var item = await GetLastChange();
        if (item is null)
        {
            item = new SettingChangeBusinessEntity
            {
                LastChange = DateTime.UtcNow
            };
            await Save(item);
        }
        else
        {
            item.LastChange = DateTime.UtcNow;
            await Update(item);
        }
    }
}