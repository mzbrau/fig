namespace Fig.Web.Services.Assistant;

public interface IDashboardAssistantActionQueue
{
    event Action? ActionsQueued;

    void EnqueueInlineScriptUpdate(string componentId, string script, Guid? dashboardId = null);

    /// <summary>
    /// Removes and returns actions for <paramref name="dashboardId"/> (and actions with no dashboard id).
    /// Actions targeting a different dashboard remain queued without raising <see cref="ActionsQueued"/>.
    /// </summary>
    IReadOnlyList<DashboardAssistantQueuedAction> DequeueForDashboard(Guid dashboardId);
}

public sealed class DashboardAssistantQueuedAction
{
    public DashboardAssistantQueuedAction(string componentId, string script, Guid? dashboardId = null)
    {
        ComponentId = componentId;
        Script = script;
        DashboardId = dashboardId;
    }

    public string ComponentId { get; }

    public string Script { get; }

    public Guid? DashboardId { get; }
}
