using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ClientRunSessionRepository : RepositoryBase<ClientRunSessionBusinessEntity>, IClientRunSessionRepository
{
    public ClientRunSessionRepository(ISession session) : base(session)
    {
    }

    public ClientRunSessionBusinessEntity? GetRunSession(Guid id)
    {
        var criteria = Session.CreateCriteria<ClientRunSessionBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ClientRunSessionBusinessEntity.RunSessionId), id));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = criteria.UniqueResult<ClientRunSessionBusinessEntity>();
        return client;
    }

    public void UpdateRunSession(ClientRunSessionBusinessEntity runSession)
    {
        Update(runSession);
    }
}
