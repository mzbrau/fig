using Humanizer;

namespace Fig.Web.Models.Clients;

public class ClientRunSessionModel
{
    public ClientRunSessionModel(string name,
        string? instance,
        DateTime? lastRegistration,
        DateTime? lastSettingValueUpdate,
        Guid runSessionId,
        DateTime? lastSeen,
        bool? liveReload,
        double? pollIntervalMs,
        double uptimeSeconds,
        string? ipAddress,
        string? hostname,
        string? figVersion,
        string? applicationVersion,
        bool offlineSettingsEnabled,
        bool supportsRestart,
        bool restartRequested,
        string runningUser,
        long memoryUsageBytes,
        bool hasConfigurationError)
    {
        Name = name;
        Instance = instance;
        LastRegistration = lastRegistration;
        LastSettingValueUpdate = lastSettingValueUpdate;
        RunSessionId = runSessionId;
        LastSeen = lastSeen;
        LiveReload = liveReload;
        PollIntervalMs = pollIntervalMs;
        UptimeSeconds = uptimeSeconds;
        IpAddress = ipAddress;
        Hostname = hostname;
        FigVersion = figVersion;
        ApplicationVersion = applicationVersion;
        OfflineSettingsEnabled = offlineSettingsEnabled;
        SupportsRestart = supportsRestart;
        RestartRequested = restartRequested;
        RunningUser = runningUser;
        MemoryUsageBytes = memoryUsageBytes;
        HasConfigurationError = hasConfigurationError;
    }

    public string Name { get; }

    public string? Instance { get; }

    public DateTime? LastRegistration { get; }

    public string LastRegistrationRelative => LastRegistration.Humanize();

    public DateTime? LastSettingValueUpdate { get; }

    public string LastSettingValueUpdateRelative => LastSettingValueUpdate.Humanize();

    public Guid RunSessionId { get; }

    public DateTime? LastSeen { get; }

    public string LastSeenRelative => LastSeen.Humanize();

    public bool? LiveReload { get; }

    public double? PollIntervalMs { get; }

    public string PollIntervalHuman => PollIntervalMs.HasValue
        ? TimeSpan.FromMilliseconds(PollIntervalMs.Value).Humanize()
        : string.Empty;

    public double UptimeSeconds { get; }

    public string UptimeSecondsHuman => TimeSpan.FromSeconds(UptimeSeconds).Humanize();

    public string? IpAddress { get; }

    public string? Hostname { get; }

    public string? FigVersion { get; }

    public string? ApplicationVersion { get; }

    public bool OfflineSettingsEnabled { get; }

    public bool RunningLatestSettings =>
        LastSettingValueUpdate == null || LastSeen > LastSettingValueUpdate && LiveReload == true;

    public bool SupportsRestart { get; }

    public bool DoesNotSupportRestart => !SupportsRestart;

    public bool RestartRequested { get; set; }

    public string RunningUser { get; }

    public long MemoryUsageBytes { get; }

    public string MemoryUsage => MemoryUsageBytes.Bytes().Humanize();
    
    public bool HasConfigurationError { get; }
}