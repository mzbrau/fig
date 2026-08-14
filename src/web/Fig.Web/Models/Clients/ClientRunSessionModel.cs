using System.Linq;
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
        DateTime lastSettingLoadUtc,
        RunSessionHealthModel health,
        IReadOnlyList<CustomStatusPropertyModel> customProperties,
        double? uptimePercent24Hr = null)
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
        LastSettingLoadUtc = lastSettingLoadUtc;
        Health = health;
        CustomProperties = customProperties;
        UptimePercent24Hr = uptimePercent24Hr;
        UiCustomProperties = customProperties
            .Where(p => p.ShowInUi)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Name)
            .ToList();
        HighlightedCustomProperties = UiCustomProperties.Where(p => p.Highlight).ToList();
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

    /// <summary>Approximate client rolling 24h uptime percentage, or null before first observation.</summary>
    public double? UptimePercent24Hr { get; }

    public string UptimePercent24HrDisplay =>
        UptimePercent24Hr.HasValue ? $"{UptimePercent24Hr.Value:0.0}%" : "—";

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

    public DateTime LastSettingLoadUtc { get; }
    
    public DateTime LastSettingLoadLocal => LastSettingLoadUtc.ToLocalTime();
    
    public RunSessionHealthModel Health { get; }

    public IReadOnlyList<CustomStatusPropertyModel> CustomProperties { get; }

    public IReadOnlyList<CustomStatusPropertyModel> UiCustomProperties { get; }

    public IReadOnlyList<CustomStatusPropertyModel> HighlightedCustomProperties { get; }

    public string CustomPropertiesSummary
    {
        get
        {
            if (HighlightedCustomProperties.Count > 0)
                return string.Join(" · ", HighlightedCustomProperties.Select(p => p.Summary));

            if (UiCustomProperties.Count > 0)
                return $"{UiCustomProperties.Count} properties";

            return "—";
        }
    }

    public bool HasExpandableDetails =>
        Health.Components.Any() || UiCustomProperties.Count > 0;
}