using Humanizer;

namespace Fig.Web.Models.Api;

public class ApiStatusModel
{
    public Guid RuntimeId { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public string Uptime => (DateTime.UtcNow - StartTimeUtc).Humanize();

    public DateTime LastSeen { get; set; }

    public string LastSeenRelative => LastSeen.Humanize();

    public string? IpAddress { get; set; }

    public string? Hostname { get; set; }

    public long MemoryUsageBytes { get; set; }

    public string MemoryUsage => MemoryUsageBytes.Bytes().Humanize();

    public string RunningUser { get; set; }

    public long TotalRequests { get; set; }

    public double RequestsPerMinute { get; set; }

    public string Version { get; set; }
    
    public bool ConfigurationErrorDetected { get; set; }
    
    public int NumberOfPluginVerifiers { get; set; }

    public string PluginVerifiers { get; set; } = string.Empty;
}