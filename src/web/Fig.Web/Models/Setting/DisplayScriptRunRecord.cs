namespace Fig.Web.Models.Setting;

public class DisplayScriptRunRecord
{
    public DateTime Timestamp { get; init; }

    public string? ClientName { get; init; }

    public string SettingName { get; init; } = string.Empty;

    public string Script { get; init; } = string.Empty;

    public string FormattedScript { get; init; } = string.Empty;

    public long DurationMs { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Precomputed lowercase haystack for free-text filtering.
    /// </summary>
    public string SearchText { get; init; } = string.Empty;
}
