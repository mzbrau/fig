using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class WebHookClientRepository : RepositoryBase<WebHookClientBusinessEntity>, IWebHookClientRepository
{
    private readonly IEncryptionService _encryptionService;

    public WebHookClientRepository(IFigSessionFactory sessionFactory, IEncryptionService encryptionService) 
        : base(sessionFactory)
    {
        _encryptionService = encryptionService;
    }
    
    public IEnumerable<WebHookClientBusinessEntity> GetClients()
    {
        var clients = GetAll();
        foreach (var client in clients)
        {
            client.Decrypt(_encryptionService);
            yield return client;
        }
    }

    public IEnumerable<WebHookClientBusinessEntity> GetClients(IEnumerable<Guid> clientIds)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<WebHookClientBusinessEntity>();
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
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<WebHookClientBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", id));
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