using System;
using Fig.Contracts.Health;

namespace Fig.Contracts.Status
{
    public class ClientRunSessionDataContract
    {
        public ClientRunSessionDataContract(Guid runSessionId, DateTime? lastSeen, bool liveReload,
            double pollIntervalMs, DateTime startTimeUtc, string? ipAddress, string? hostname, string figVersion,
            string applicationVersion, bool offlineSettingsEnabled, bool supportsRestart, bool restartRequested,
            bool restartRequiredToApplySettings,
            string runningUser, long memoryUsageBytes,
            DateTime lastSettingLoadUtc, HealthDataContract? health = null)
        {
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
        }

        public Guid RunSessionId { get; }

        public DateTime? LastSeen { get; }

        public bool LiveReload { get; }
        
        public DateTime LastSettingLoadUtc { get; }

        public double PollIntervalMs { get; }

        public DateTime StartTimeUtc { get; }

        public string? IpAddress { get; }

        public string? Hostname { get; }

        public string FigVersion { get; }

        public string ApplicationVersion { get; }

        public bool OfflineSettingsEnabled { get; }

        public bool SupportsRestart { get; }

        public bool RestartRequested { get; }
        
        public bool RestartRequiredToApplySettings { get; }

        public string RunningUser { get; }

        public long MemoryUsageBytes { get; }

        public HealthDataContract? Health { get; set; }
    }
}