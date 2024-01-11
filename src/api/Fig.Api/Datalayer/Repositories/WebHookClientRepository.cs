using Fig.Api.ExtensionMethods;
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
    
    public IEnumerable<WebHookClientBusinessEntity> GetClients(bool upgradeLock)
    {
        var clients = GetAll(upgradeLock);
        foreach (var client in clients)
        {
            client.Decrypt(_encryptionService);
            yield return client;
        }
    }

    public IEnumerable<WebHookClientBusinessEntity> GetClients(IEnumerable<Guid> clientIds)
    {
        var criteria = Session.CreateCriteria<WebHookClientBusinessEntity>();
        criteria.Add(Restrictions.In("Id", clientIds.ToArray()));
        var webHookClients = criteria.List<WebHookClientBusinessEntity>();
        foreach (var client in webHookClients)
        {
            client.Decrypt(_encryptionService);
        }
        return webHookClients;
    }

    public Guid AddClient(WebHookClientBusinessEntity client)
    {
        client.Encrypt(_encryptionService);
        return Save(client);
    }

    public void DeleteClient(Guid clientId)
    {
        var client = GetClient(clientId);
        
        if (client != null)
            Delete(client);
    }

    public WebHookClientBusinessEntity? GetClient(Guid id)
    {
        var criteria = Session.CreateCriteria<WebHookClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", id));
        criteria.SetLockMode(LockMode.Upgrade);
        var client = criteria.UniqueResult<WebHookClientBusinessEntity>();
        client.Decrypt(_encryptionService);
        return client;
    }

    public void UpdateClient(WebHookClientBusinessEntity client)
    {
        client.Encrypt(_encryptionService);
        Update(client);
    }
}