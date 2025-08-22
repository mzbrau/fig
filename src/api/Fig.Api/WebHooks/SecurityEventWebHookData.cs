namespace Fig.Api.WebHooks;

public record SecurityEventWebHookData(
    string EventType,
    DateTime Timestamp,
    string? Username,
    bool Success,
    string? IpAddress,
    string? Hostname,
    string? FailureReason = null);