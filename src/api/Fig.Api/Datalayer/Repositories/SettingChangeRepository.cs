using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public class SettingChangeRepository : RepositoryBase<SettingChangeBusinessEntity>, ISettingChangeRepository
{
    public SettingChangeRepository(IFigSessionFactory sessionFactory) 
        : base(sessionFactory)
    {
    }
    
    public SettingChangeBusinessEntity? GetLastChange()
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<SettingChangeBusinessEntity>();
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