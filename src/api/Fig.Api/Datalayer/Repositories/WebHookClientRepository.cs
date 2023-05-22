using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class WebHookClientRepository : RepositoryBase<WebHookClientBusinessEntity>, IWebHookClientRepository
{
    public WebHookClientRepository(IFigSessionFactory sessionFactory) 
        : base(sessionFactory)
    {
    }
    
    public IEnumerable<WebHookClientBusinessEntity> GetClients()
    {
        return GetAll();
    }

    public Guid AddClient(WebHookClientBusinessEntity client)
    {
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
        return client;
    }

    public void UpdateClient(WebHookClientBusinessEntity client)
    {
        Update(client);
    }
}