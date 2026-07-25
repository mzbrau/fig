namespace Fig.Api.Datalayer.Repositories;

/// <summary>
/// Optional filters for event log queries. All filters are AND-combined with the time range.
/// </summary>
public sealed class EventLogQuery
{
    public string? ClientName { get; init; }

    public string? Instance { get; init; }

    public string? AuthenticatedUser { get; init; }

    public IReadOnlyCollection<string>? EventTypes { get; init; }

    /// <summary>
    /// Case-insensitive substring match across plaintext columns only
    /// (Message, SettingName, ClientName, AuthenticatedUser, EventType).
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Caps rows returned from the database before decrypt. Null means no cap.
    /// </summary>
    public int? MaxResults { get; init; }
}
