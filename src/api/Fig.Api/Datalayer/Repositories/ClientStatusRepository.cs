using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ClientStatusRepository : RepositoryBase<ClientStatusBusinessEntity>, IClientStatusRepository
{
    public ClientStatusRepository(ISession session)
        : base(session)
    {
    }

    public async Task<ClientStatusBusinessEntity?> GetClient(string name, string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ClientStatusBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ClientStatusBusinessEntity.Name), name));
        criteria.Add(Restrictions.Eq(nameof(ClientStatusBusinessEntity.Instance), instance));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = await criteria.UniqueResultAsync<ClientStatusBusinessEntity>();
        return client;
    }

    public async Task<ClientStatusBusinessEntity?> GetClientReadOnly(string name, string? instance = null)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ClientStatusBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ClientStatusBusinessEntity.Name), name));
        criteria.Add(Restrictions.Eq(nameof(ClientStatusBusinessEntity.Instance), instance));
        // No LockMode.Upgrade for read-only operations to reduce contention
        var client = await criteria.UniqueResultAsync<ClientStatusBusinessEntity>();
        
        if (client != null)
        {
            await Session.EvictAsync(client);
        }
        
        return client;
    }

    public async Task UpdateClientStatus(ClientStatusBusinessEntity clientStatus)
    {
        await Update(clientStatus);
    }

    public async Task<IList<ClientStatusBusinessEntity>> GetAllClients(UserDataContract? requestingUser)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        return (await GetAll(false))
            .Where(session => requestingUser?.HasAccess(session.Name) == true)
            .ToList();
    }
}