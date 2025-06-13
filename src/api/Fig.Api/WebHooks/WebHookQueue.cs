using System.Collections.Concurrent;

namespace Fig.Api.WebHooks;

public class WebHookQueue : IWebHookQueue
{
    private readonly ConcurrentQueue<WebHookQueueItem> _queue = new();
    private int _count;

    public void QueueWebHook(WebHookQueueItem item)
    {
        _queue.Enqueue(item);
        Interlocked.Increment(ref _count);
    }

    public WebHookQueueItem? DequeueWebHook()
    {
        if (_queue.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref _count);
            return item;
        }
        
        return null;
    }
}
