using Radzen;

namespace Fig.Web.Notifications;

public class NotificationRecord
{
    public DateTime Timestamp { get; init; }

    public NotificationSeverity Severity { get; init; }

    public string Summary { get; init; } = string.Empty;

    public string? Detail { get; init; }
}
