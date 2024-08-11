using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class WebHookRepository : RepositoryBase<WebHookBusinessEntity>, IWebHookRepository
{
    public WebHookRepository(ISession session) 
        : base(session)
    {
    }
    
    public IList<WebHookBusinessEntity> GetWebHooks()
    {
        return GetAll(false);
    }

    public IList<WebHookBusinessEntity> GetWebHooksForClient(Guid clientId)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("ClientId", clientId));
        var webHooks = criteria.List<WebHookBusinessEntity>();
        return webHooks;
    }
    
    public IList<WebHookBusinessEntity> GetWebHooksByType(WebHookType webHookType)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
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
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", webHookId));
        criteria.SetLockMode(LockMode.Upgrade);
        var webHook = criteria.UniqueResult<WebHookBusinessEntity>();
        return webHook;
    }

    public void UpdateWebHook(WebHookBusinessEntity webHook)
    {
        Update(webHook);
    }
}