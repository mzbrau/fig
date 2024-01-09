using Fig.Datalayer.BusinessEntities;
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
        var criteria = Session.CreateCriteria<SettingChangeBusinessEntity>();
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