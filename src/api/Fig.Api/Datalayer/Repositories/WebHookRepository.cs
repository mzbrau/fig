using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class WebHookRepository : RepositoryBase<WebHookBusinessEntity>, IWebHookRepository
{
    public WebHookRepository(IFigSessionFactory sessionFactory) 
        : base(sessionFactory)
    {
    }
    
    public IEnumerable<WebHookBusinessEntity> GetWebHooks()
    {
        return GetAll();
    }

    public IEnumerable<WebHookBusinessEntity> GetWebHooksForClient(Guid clientId)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("ClientId", clientId));
        var webHooks = criteria.List<WebHookBusinessEntity>();
        return webHooks;
    }

    public Guid AddWebHook(WebHookBusinessEntity webHook)
    {
        return Save(webHook);
    }

    public void DeleteWebHook(Guid webHookId)
    {
        var webHook = GetWebHook(webHookId);
        
        if (webHook != null)
            Delete(webHook);
    }

    public WebHookBusinessEntity? GetWebHook(Guid webHookId)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", webHookId));
        var webHook = criteria.UniqueResult<WebHookBusinessEntity>();
        return webHook;
    }

    public void UpdateWebHook(WebHookBusinessEntity webHook)
    {
        Update(webHook);
    }
}