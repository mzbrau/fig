namespace Fig.Api.WebHooks;

public interface IWebHookQueue
{
    void QueueWebHook(WebHookQueueItem item);
    
    WebHookQueueItem? DequeueWebHook();
}