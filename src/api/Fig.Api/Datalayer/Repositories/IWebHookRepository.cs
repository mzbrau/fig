using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IWebHookRepository
{
    IEnumerable<WebHookBusinessEntity> GetWebHooks();
    
    Guid AddWebHook(WebHookBusinessEntity webHook);

    void DeleteWebHook(Guid webHookId);
    
    WebHookBusinessEntity? GetWebHook(Guid webHookId);
    
    void UpdateWebHook(WebHookBusinessEntity webHook);
}