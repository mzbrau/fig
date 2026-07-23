using System.Collections.Concurrent;

namespace Fig.Web.Services.Assistant;

public sealed class AssistantUiActionQueue : IAssistantUiActionQueue
{
    private readonly ConcurrentQueue<AssistantUiQueuedAction> _queue = new();

    public event Action? ActionsQueued;

    public void EnqueueSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query is required.", nameof(query));

        _queue.Enqueue(AssistantUiQueuedAction.Search(query.Trim()));
        ActionsQueued?.Invoke();
    }

    public void EnqueueHighlight(string clientName, string settingName, string? instance)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("Client name is required.", nameof(clientName));
        if (string.IsNullOrWhiteSpace(settingName))
            throw new ArgumentException("Setting name is required.", nameof(settingName));

        _queue.Enqueue(AssistantUiQueuedAction.Highlight(clientName.Trim(), settingName.Trim(), instance));
        ActionsQueued?.Invoke();
    }

    public IReadOnlyList<AssistantUiQueuedAction> DequeueAll()
    {
        var actions = new List<AssistantUiQueuedAction>();
        while (_queue.TryDequeue(out var action))
            actions.Add(action);
        return actions;
    }
}
