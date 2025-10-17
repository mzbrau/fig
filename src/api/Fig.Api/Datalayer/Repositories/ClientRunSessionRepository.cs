using System.Diagnostics;
using Fig.Api.Observability;
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

    public async Task<ClientRunSessionBusinessEntity?> GetRunSession(Guid id)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ClientRunSessionBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ClientRunSessionBusinessEntity.RunSessionId), id));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = await criteria.UniqueResultAsync<ClientRunSessionBusinessEntity>();
        return client;
    }

    public async Task UpdateRunSession(ClientRunSessionBusinessEntity runSession)
    {
        await Update(runSession);
    }

    public async Task DeleteRunSession(ClientRunSessionBusinessEntity runSession)
    {
        await Delete(runSession);
    }
}
