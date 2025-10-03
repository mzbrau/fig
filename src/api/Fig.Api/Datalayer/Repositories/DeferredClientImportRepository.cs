using System.Diagnostics;
using Fig.Api.Exceptions;
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

    public async Task<IList<DeferredClientImportBusinessEntity>> GetClients(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<DeferredClientImportBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        var clients = await criteria.ListAsync<DeferredClientImportBusinessEntity>();
        return clients;
    }

    public async Task AddClient(DeferredClientImportBusinessEntity client)
    {
        if (client.ImportTime < (DateTime.UtcNow - TimeSpan.FromDays(365)))
            throw new InvalidImportException(
                $"Import for client {client.Name} is older than 1 year and will not be imported");
        
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
        var results = (await GetAll(false))
            .Where(client => requestingUser?.HasAccess(client.Name) == true)
            .ToList();
        return results;
    }

    public async Task DeleteAll()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var allClients = await GetAll(false);
        foreach (var client in allClients)
        {
            await Delete(client);
        }
    }
}