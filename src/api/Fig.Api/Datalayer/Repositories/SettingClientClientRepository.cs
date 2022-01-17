using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

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
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        return criteria.UniqueResult<SettingClientBusinessEntity>();
    }

    public IEnumerable<SettingClientBusinessEntity> GetAllInstancesOfClient(string name)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<SettingClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        return criteria.List<SettingClientBusinessEntity>();
    }

    public void DeleteClient(SettingClientBusinessEntity client)
    {
        Delete(client);
    }
}