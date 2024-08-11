using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IWebHookRepository
{
    IList<WebHookBusinessEntity> GetWebHooks();

    IList<WebHookBusinessEntity> GetWebHooksForClient(Guid clientId);

    IList<WebHookBusinessEntity> GetWebHooksByType(WebHookType webHookType);

    Guid AddWebHook(WebHookBusinessEntity webHook);

    void DeleteWebHook(Guid webHookId);
    
    WebHookBusinessEntity? GetWebHook(Guid webHookId);
    
    void UpdateWebHook(WebHookBusinessEntity webHook);
}