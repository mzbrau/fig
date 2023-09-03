using Fig.Web.Attributes;
using Humanizer;

namespace Fig.Web.MarkdownReport;

public class RunSession
{
    public RunSession(string name, string? instance, DateTime? lastSettingValueUpdate, double uptimeSeconds, string? ipAddress, string? hostname, bool offlineSettingsEnabled, string runningUser, long memoryUsageBytes, bool configError)
    {
        Name = name;
        Instance = instance;
        LastSettingValueUpdate = lastSettingValueUpdate;
        UptimeSeconds = uptimeSeconds;
        IpAddress = ipAddress;
        Hostname = hostname;
        OfflineSettingsEnabled = offlineSettingsEnabled;
        RunningUser = runningUser;
        MemoryUsageBytes = memoryUsageBytes;
        ConfigError = configError;
    }

    [Order(1)]
    [Sort]
    public string Name { get; }

    [Order(2)]
    public string? Instance { get; }

    private DateTime? LastSettingValueUpdate { get; }

    [Order(3)]
    public string LastSettingChange => LastSettingValueUpdate.Humanize();

    private double UptimeSeconds { get; }

    [Order(4)]
    public string Uptime => TimeSpan.FromSeconds(UptimeSeconds).Humanize();

    [Order(5)]
    public string? IpAddress { get; }

    [Order(6)]
    public string? Hostname { get; }

    [Order(7)]
    public bool OfflineSettingsEnabled { get; }

    [Order(8)]
    public string RunningUser { get; }

    private long MemoryUsageBytes { get; }

    [Order(9)]
    public string MemoryUsage => MemoryUsageBytes.Bytes().Humanize();
    
    [Order(10)]
    public bool ConfigError { get; }

}