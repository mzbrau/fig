namespace Fig.Web.Services.Assistant;

public interface IAssistantUiActionQueue
{
    event Action? ActionsQueued;

    void EnqueueSearch(string query);

    void EnqueueHighlight(string clientName, string settingName, string? instance);

    IReadOnlyList<AssistantUiQueuedAction> DequeueAll();
}

public enum AssistantUiActionKind
{
    Search,
    Highlight
}

public sealed class AssistantUiQueuedAction
{
    private AssistantUiQueuedAction(AssistantUiActionKind kind, string? searchQuery, string? clientName, string? settingName, string? instance)
    {
        Kind = kind;
        SearchQuery = searchQuery;
        ClientName = clientName;
        SettingName = settingName;
        Instance = instance;
    }

    public AssistantUiActionKind Kind { get; }

    public string? SearchQuery { get; }

    public string? ClientName { get; }

    public string? SettingName { get; }

    public string? Instance { get; }

    public static AssistantUiQueuedAction Search(string query) =>
        new(AssistantUiActionKind.Search, query, null, null, null);

    public static AssistantUiQueuedAction Highlight(string clientName, string settingName, string? instance) =>
        new(AssistantUiActionKind.Highlight, null, clientName, settingName, instance);
}
