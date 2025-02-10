using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IWebHookRepository
{
    Task<IList<WebHookBusinessEntity>> GetWebHooks();

    Task<IList<WebHookBusinessEntity>> GetWebHooksForClient(Guid clientId);

    Task<IList<WebHookBusinessEntity>> GetWebHooksByType(WebHookType webHookType);

    Task<Guid> AddWebHook(WebHookBusinessEntity webHook);

    Task DeleteWebHook(Guid webHookId);
    
    Task<WebHookBusinessEntity?> GetWebHook(Guid webHookId);
    
    Task UpdateWebHook(WebHookBusinessEntity webHook);
}