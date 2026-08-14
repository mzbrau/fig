using System.Collections.Concurrent;

namespace Fig.Web.Services.Assistant;

public sealed class DashboardAssistantActionQueue : IDashboardAssistantActionQueue
{
    private readonly ConcurrentQueue<DashboardAssistantQueuedAction> _queue = new();

    public event Action? ActionsQueued;

    public void EnqueueInlineScriptUpdate(string componentId, string script, Guid? dashboardId = null)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentException("Component id is required.", nameof(componentId));
        if (script is null)
            throw new ArgumentNullException(nameof(script));

        _queue.Enqueue(new DashboardAssistantQueuedAction(componentId.Trim(), script, dashboardId));
        ActionsQueued?.Invoke();
    }

    public IReadOnlyList<DashboardAssistantQueuedAction> DequeueForDashboard(Guid dashboardId)
    {
        var drained = new List<DashboardAssistantQueuedAction>();
        while (_queue.TryDequeue(out var action))
            drained.Add(action);

        var matching = new List<DashboardAssistantQueuedAction>();
        foreach (var action in drained)
        {
            if (action.DashboardId is null || action.DashboardId == dashboardId)
                matching.Add(action);
            else
                _queue.Enqueue(action);
        }

        return matching;
    }
}
