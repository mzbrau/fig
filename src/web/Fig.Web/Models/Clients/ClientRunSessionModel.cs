using Humanizer;

namespace Fig.Web.Models.Clients;

public class ClientRunSessionModel
{
    public ClientRunSessionModel(string name,
        string? instance,
        DateTime? lastRegistration,
        DateTime? lastSettingValueUpdateUtc,
        Guid runSessionId,
        DateTime? lastSeen,
        bool liveReload,
        double pollIntervalMs,
        DateTime startTimeUtc,
        string? ipAddress,
        string? hostname,
        string? figVersion,
        string? applicationVersion,
        bool offlineSettingsEnabled,
        bool supportsRestart,
        bool restartRequested,
        bool restartRequiredToApplySettings,
        string runningUser,
        long memoryUsageBytes,
        bool hasConfigurationError, 
        DateTime lastSettingLoadUtc)
    {
        Name = name;
        Instance = instance;
        LastRegistration = lastRegistration;
        LastSettingValueUpdateUtc = lastSettingValueUpdateUtc;
        RunSessionId = runSessionId;
        LastSeen = lastSeen;
        LiveReload = liveReload;
        PollIntervalMs = pollIntervalMs;
        StartTimeUtc = startTimeUtc;
        IpAddress = ipAddress;
        Hostname = hostname;
        FigVersion = figVersion;
        ApplicationVersion = applicationVersion;
        OfflineSettingsEnabled = offlineSettingsEnabled;
        SupportsRestart = supportsRestart;
        RestartRequested = restartRequested;
        RestartRequiredToApplySettings = restartRequiredToApplySettings;
        RunningUser = runningUser;
        MemoryUsageBytes = memoryUsageBytes;
        HasConfigurationError = hasConfigurationError;
        LastSettingLoadUtc = lastSettingLoadUtc;
    }

    public string Name { get; }

    public string? Instance { get; }

    public DateTime? LastRegistration { get; }

    public string LastRegistrationRelative => LastRegistration.Humanize();

    public DateTime? LastSettingValueUpdateUtc { get; }
    
    public DateTime? LastSettingValueUpdate => LastSettingValueUpdateUtc?.ToLocalTime();

    public string LastSettingValueUpdateRelative => LastSettingValueUpdate.Humanize();

    public Guid RunSessionId { get; }

    public DateTime? LastSeen { get; }

    public string LastSeenRelative => LastSeen.Humanize();

    public bool LiveReload { get; set; }

    public double PollIntervalMs { get; }

    public string PollIntervalHuman => TimeSpan.FromMilliseconds(PollIntervalMs).Humanize();

    public DateTime StartTimeUtc { get; }
    
    public DateTime StartTimeLocal => StartTimeUtc.ToLocalTime();

    public string UptimeHuman => (DateTime.UtcNow - StartTimeUtc).Humanize();

    public string? IpAddress { get; }

    public string? Hostname { get; }

    public string? FigVersion { get; }

    public string? ApplicationVersion { get; }

    public bool OfflineSettingsEnabled { get; }

    public bool RunningLatestSettings =>
        LastSettingValueUpdateUtc == null || LastSettingLoadUtc > LastSettingValueUpdateUtc == true;

    public bool SupportsRestart { get; }

    public bool DoesNotSupportRestart => !SupportsRestart;

    public bool RestartRequested { get; set; }
    
    public bool RestartRequiredToApplySettings { get; set; }

    public string RunningUser { get; }

    public long MemoryUsageBytes { get; }

    public string MemoryUsage => MemoryUsageBytes.Bytes().Humanize();
    
    public bool HasConfigurationError { get; }
    
    public DateTime LastSettingLoadUtc { get; }
    
    public DateTime LastSettingLoadLocal => LastSettingLoadUtc.ToLocalTime();
}