using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class ClientStatusRepository : RepositoryBase<ClientStatusBusinessEntity>, IClientStatusRepository
{
    public ClientStatusRepository(IFigSessionFactory sessionFactory)
        : base(sessionFactory)
    {
    }

    public ClientStatusBusinessEntity? GetClient(string name, string? instance = null)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<ClientStatusBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        var client = criteria.UniqueResult<ClientStatusBusinessEntity>();
        return client;
    }

    public void UpdateClientStatus(ClientStatusBusinessEntity clientStatus)
    {
        Update(clientStatus);
    }

    public IEnumerable<ClientStatusBusinessEntity> GetAllClients()
    {
        return GetAll();
    }
}