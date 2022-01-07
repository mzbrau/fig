using Fig.Api.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingHistoryRepository : RepositoryBase<SettingValueBusinessEntity>, ISettingHistoryRepository
{
    public SettingHistoryRepository(IFigSessionFactory sessionFactory) 
        : base(sessionFactory)
    {
    }


    public void Add(SettingValueBusinessEntity settingValue)
    {
        Save(settingValue);
    }

    public IEnumerable<SettingValueBusinessEntity> GetAll(Guid clientId, string settingName)
    {
        using ISession session = SessionFactory.OpenSession();
        ICriteria criteria = session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("ClientId", clientId));
        criteria.Add(Restrictions.Eq("SettingName", settingName));
        return criteria.List<SettingValueBusinessEntity>();
    }
}