using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class WebHookRepository : RepositoryBase<WebHookBusinessEntity>, IWebHookRepository
{
    public WebHookRepository(ISession session) 
        : base(session)
    {
    }
    
    public IEnumerable<WebHookBusinessEntity> GetWebHooks()
    {
        return GetAll();
    }

    public IEnumerable<WebHookBusinessEntity> GetWebHooksForClient(Guid clientId)
    {
        var criteria = Session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("ClientId", clientId));
        var webHooks = criteria.List<WebHookBusinessEntity>();
        return webHooks;
    }
    
    public IEnumerable<WebHookBusinessEntity> GetWebHooksByType(WebHookType webHookType)
    {
        var criteria = Session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("WebHookType", webHookType));
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
        var criteria = Session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", webHookId));
        var webHook = criteria.UniqueResult<WebHookBusinessEntity>();
        return webHook;
    }

    public void UpdateWebHook(WebHookBusinessEntity webHook)
    {
        Update(webHook);
    }
}