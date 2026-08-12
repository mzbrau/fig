namespace Fig.Web.Services.Assistant;

public interface IDashboardAssistantActionQueue
{
    event Action? ActionsQueued;

    void EnqueueInlineScriptUpdate(string componentId, string script);

    IReadOnlyList<DashboardAssistantQueuedAction> DequeueAll();
}

public sealed class DashboardAssistantQueuedAction
{
    public DashboardAssistantQueuedAction(string componentId, string script)
    {
        ComponentId = componentId;
        Script = script;
    }

    public string ComponentId { get; }

    public string Script { get; }
}
