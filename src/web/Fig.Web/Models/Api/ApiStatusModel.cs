using Humanizer;

namespace Fig.Web.Models.Api;

public class ApiStatusModel
{
    public Guid RuntimeId { get; set; }

    public DateTime StartTimeUtc { get; init; }

    public string Uptime => (DateTime.UtcNow - StartTimeUtc).Humanize();

    public DateTime LastSeen { get; init; }

    public string LastSeenRelative => LastSeen.Humanize();

    public string? IpAddress { get; set; }

    public string? Hostname { get; init; }

    public long MemoryUsageBytes { get; init; }

    public string MemoryUsage => MemoryUsageBytes.Bytes().Humanize();

    public string RunningUser { get; set; } = "Unknown";

    public long TotalRequests { get; set; }

    public double RequestsPerMinute { get; set; }

    public string Version { get; set; } = "0.0.0";
    
    public bool ConfigurationErrorDetected { get; set; }
}