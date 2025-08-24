namespace Fig.WebHooks.Contracts;

public class SecurityEventDataContract : IWebHookContract
{
    public SecurityEventDataContract(
        string eventType,
        DateTime timestamp,
        string? username,
        bool success,
        string? ipAddress,
        string? hostname,
        string? failureReason = null,
        Uri? uri = null,
        bool isTest = false)
    {
        EventType = eventType;
        Timestamp = timestamp;
        Username = username;
        Success = success;
        IpAddress = ipAddress;
        Hostname = hostname;
        FailureReason = failureReason;
        Uri = uri;
        IsTest = isTest;
    }

    /// <summary>
    /// The type of security event (e.g., "Login", "Failed Login")
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// UTC timestamp when the security event occurred
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Username associated with the security event (if available)
    /// </summary>
    public string? Username { get; }

    /// <summary>
    /// Whether the security operation was successful
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// IP address of the client making the request
    /// </summary>
    public string? IpAddress { get; }

    /// <summary>
    /// Hostname of the client making the request
    /// </summary>
    public string? Hostname { get; }

    /// <summary>
    /// Reason for failure if Success is false
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// URI for navigation to related information in the Fig web application
    /// </summary>
    public Uri? Uri { get; }
    
    /// <summary>
    /// Indicates whether this webhook is a test webhook or a real event
    /// </summary>
    public bool IsTest { get; }
}