using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class DeferredClientImportRepository : RepositoryBase<DeferredClientImportBusinessEntity>, IDeferredClientImportRepository
{
    public DeferredClientImportRepository(ISession session) 
        : base(session)
    {
    }

    public async Task<IList<DeferredClientImportBusinessEntity>> GetClients(string name, string? instance)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<DeferredClientImportBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        criteria.Add(Restrictions.Eq("Instance", instance));
        var clients = await criteria.ListAsync<DeferredClientImportBusinessEntity>();
        return clients;
    }

    public async Task AddClient(DeferredClientImportBusinessEntity client)
    {
        await Save(client);
    }

    public async Task DeleteClient(Guid id)
    {
        var existing = await Get(id, true);
        if (existing != null)
            await Delete(existing);
    }

    public async Task<IList<DeferredClientImportBusinessEntity>> GetAllClients(UserDataContract? requestingUser)
    {
        return (await GetAll(false))
            .Where(client => requestingUser?.HasAccess(client.Name) == true)
            .ToList();
    }
}