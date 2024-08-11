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
    
    public SettingChangeBusinessEntity? GetLastChange()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingChangeBusinessEntity>();
        criteria.SetLockMode(LockMode.Upgrade);
        var result = criteria.UniqueResult<SettingChangeBusinessEntity>();
        return result;
    }

    public void RegisterChange()
    {
        var item = GetLastChange();
        if (item is null)
        {
            item = new SettingChangeBusinessEntity
            {
                LastChange = DateTime.UtcNow
            };
            Save(item);
        }
        else
        {
            item.LastChange = DateTime.UtcNow;
            Update(item);
        }
    }
}