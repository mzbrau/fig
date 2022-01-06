using Fig.Api.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingClientClientRepository : RepositoryBase<SettingClientBusinessEntity>, ISettingClientRepository
{
    public SettingClientClientRepository(IFigSessionFactory sessionFactory) : base(sessionFactory)
    {
    }

    public Guid RegisterClient(SettingClientBusinessEntity client)
    {
        return Save(client);
    }

    public void UpdateClient(SettingClientBusinessEntity client)
    {
        Update(client);
    }

    public IEnumerable<SettingClientBusinessEntity> GetAllClients()
    {
        return GetAll();
    }

    public SettingClientBusinessEntity? GetClient(Guid id)
    {
        return Get(id);
    }

    public SettingClientBusinessEntity? GetClient(string name, string? instance = null)
    {
        using ISession session = SessionFactory.OpenSession();
        ICriteria criteria = session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("name", name));
        criteria.Add(Restrictions.Eq("instance", instance));
        return criteria.UniqueResult<SettingClientBusinessEntity>();
    }
}