using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.WebHooks;

public class WebHookQueueItem
{
    public WebHookType WebHookType { get; set; }
    
    public object WebHookData { get; set; } = null!;
    
    public List<WebHookBusinessEntity> MatchingWebHooks { get; set; } = new();
}
