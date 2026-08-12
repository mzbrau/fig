using System.Collections.Concurrent;

namespace Fig.Web.Services.Assistant;

public sealed class DashboardAssistantActionQueue : IDashboardAssistantActionQueue
{
    private readonly ConcurrentQueue<DashboardAssistantQueuedAction> _queue = new();

    public event Action? ActionsQueued;

    public void EnqueueInlineScriptUpdate(string componentId, string script)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentException("Component id is required.", nameof(componentId));
        if (script is null)
            throw new ArgumentNullException(nameof(script));

        _queue.Enqueue(new DashboardAssistantQueuedAction(componentId.Trim(), script));
        ActionsQueued?.Invoke();
    }

    public IReadOnlyList<DashboardAssistantQueuedAction> DequeueAll()
    {
        var actions = new List<DashboardAssistantQueuedAction>();
        while (_queue.TryDequeue(out var action))
            actions.Add(action);
        return actions;
    }
}
