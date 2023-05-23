using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IWebHookRepository
{
    IEnumerable<WebHookBusinessEntity> GetWebHooks();

    IEnumerable<WebHookBusinessEntity> GetWebHooksForClient(Guid clientId);

    Guid AddWebHook(WebHookBusinessEntity webHook);

    void DeleteWebHook(Guid webHookId);
    
    WebHookBusinessEntity? GetWebHook(Guid webHookId);
    
    void UpdateWebHook(WebHookBusinessEntity webHook);
}