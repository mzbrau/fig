using Humanizer;

namespace Fig.Web.Models.Clients;

public class ClientRunSessionModel
{
    public string Name { get; set; }

    public string? Instance { get; set; }

    public DateTime? LastRegistration { get; set; }

    public string LastRegistrationRelative => LastRegistration.Humanize();

    public DateTime? LastSettingValueUpdate { get; set; }

    public string LastSettingValueUpdateRelative => LastSettingValueUpdate.Humanize();

    public Guid RunSessionId { get; set; }

    public DateTime? LastSeen { get; set; }

    public string LastSeenRelative => LastSeen.Humanize();

    public bool? LiveReload { get; set; }

    public double? PollIntervalMs { get; set; }

    public string PollIntervalHuman => PollIntervalMs.HasValue
        ? TimeSpan.FromMilliseconds(PollIntervalMs.Value).Humanize()
        : string.Empty;

    public double UptimeSeconds { get; set; }

    public string UptimeSecondsHuman => TimeSpan.FromSeconds(UptimeSeconds).Humanize();

    public string? IpAddress { get; set; }

    public string? Hostname { get; set; }

    public string? FigVersion { get; set; }

    public string? ApplicationVersion { get; set; }

    public bool OfflineSettingsEnabled { get; set; }

    public bool RunningLatestSettings =>
        LastSettingValueUpdate == null || LastSeen > LastSettingValueUpdate && LiveReload == true;

    public bool SupportsRestart { get; set; }

    public bool DoesNotSupportRestart => !SupportsRestart;

    public bool RestartRequested { get; set; }

    public string RunningUser { get; set; }

    public long MemoryUsageBytes { get; set; }

    public string MemoryUsage => MemoryUsageBytes.Bytes().Humanize();
    
    public bool HasConfigurationError { get; set; }
}