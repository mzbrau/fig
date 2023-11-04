using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class ClientRunSessionRepository : RepositoryBase<ClientRunSessionBusinessEntity>, IClientRunSessionRepository
{
    public ClientRunSessionRepository(IFigSessionFactory sessionFactory) : base(sessionFactory)
    {
    }

    public ClientRunSessionBusinessEntity? GetRunSession(Guid id)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<ClientRunSessionBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ClientRunSessionBusinessEntity.RunSessionId), id));
        var client = criteria.UniqueResult<ClientRunSessionBusinessEntity>();
        return client;
    }

    public void UpdateRunSession(ClientRunSessionBusinessEntity runSession)
    {
        Update(runSession);
    }
}
