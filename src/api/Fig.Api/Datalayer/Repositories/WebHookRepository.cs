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
    
    public async Task<IList<WebHookBusinessEntity>> GetWebHooks()
    {
        return await GetAll(false);
    }

    public async Task<IList<WebHookBusinessEntity>> GetWebHooksForClient(Guid clientId)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("ClientId", clientId));
        var webHooks = await criteria.ListAsync<WebHookBusinessEntity>();
        return webHooks;
    }
    
    public async Task<IList<WebHookBusinessEntity>> GetWebHooksByType(WebHookType webHookType)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("WebHookType", webHookType));
        var webHooks = await criteria.ListAsync<WebHookBusinessEntity>();
        return webHooks;
    }

    public async Task<Guid> AddWebHook(WebHookBusinessEntity webHook)
    {
        return await Save(webHook);
    }

    public async Task DeleteWebHook(Guid webHookId)
    {
        var webHook = await GetWebHook(webHookId);
        
        if (webHook != null)
            await Delete(webHook);
    }

    public async Task<WebHookBusinessEntity?> GetWebHook(Guid webHookId)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<WebHookBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", webHookId));
        criteria.SetLockMode(LockMode.Upgrade);
        var webHook = await criteria.UniqueResultAsync<WebHookBusinessEntity>();
        return webHook;
    }

    public async Task UpdateWebHook(WebHookBusinessEntity webHook)
    {
        await Update(webHook);
    }
}