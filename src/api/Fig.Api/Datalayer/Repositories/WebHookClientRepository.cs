using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class WebHookClientRepository : RepositoryBase<WebHookClientBusinessEntity>, IWebHookClientRepository
{
    private readonly IEncryptionService _encryptionService;

    public WebHookClientRepository(ISession session, IEncryptionService encryptionService) 
        : base(session)
    {
        _encryptionService = encryptionService;
    }
    
    public async Task<IEnumerable<WebHookClientBusinessEntity>> GetClients(bool upgradeLock)
    {
        var clients = await GetAll(upgradeLock);
        foreach (var client in clients)
        {
            client.Decrypt(_encryptionService);
        }
        return clients.ToList();
    }

    public async Task<IList<WebHookClientBusinessEntity>> GetClients(IEnumerable<Guid> clientIds)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<WebHookClientBusinessEntity>();
        criteria.Add(Restrictions.In("Id", clientIds.ToArray()));
        var webHookClients = await criteria.ListAsync<WebHookClientBusinessEntity>();
        foreach (var client in webHookClients)
        {
            client.Decrypt(_encryptionService);
        }
        return webHookClients;
    }

    public async Task<Guid> AddClient(WebHookClientBusinessEntity client)
    {
        client.Encrypt(_encryptionService);
        return await Save(client);
    }

    public async Task DeleteClient(Guid clientId)
    {
        var client = await GetClient(clientId);
        
        if (client != null)
            await Delete(client);
    }

    public async Task<WebHookClientBusinessEntity?> GetClient(Guid id)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<WebHookClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", id));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = await criteria.UniqueResultAsync<WebHookClientBusinessEntity>();
        client.Decrypt(_encryptionService);
        return client;
    }

    public async Task UpdateClient(WebHookClientBusinessEntity client)
    {
        client.Encrypt(_encryptionService);
        await Update(client);
    }
}