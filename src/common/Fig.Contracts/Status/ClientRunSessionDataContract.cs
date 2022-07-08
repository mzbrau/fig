using System;

namespace Fig.Contracts.Status
{
    public class ClientRunSessionDataContract
    {
        public ClientRunSessionDataContract(Guid runSessionId, DateTime? lastSeen, bool? liveReload,
            double? pollIntervalMs, double uptimeSeconds, string? ipAddress, string? hostname, string figVersion,
            string applicationVersion, bool offlineSettingsEnabled, bool supportsRestart, bool restartRequested,
            string runningUser, long memoryUsageBytes)
        {
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
        }

        public Guid RunSessionId { get; }

        public DateTime? LastSeen { get; }

        public bool? LiveReload { get; }

        public double? PollIntervalMs { get; }

        public double UptimeSeconds { get; }

        public string? IpAddress { get; }

        public string? Hostname { get; }

        public string FigVersion { get; }

        public string ApplicationVersion { get; }

        public bool OfflineSettingsEnabled { get; }

        public bool SupportsRestart { get; }

        public bool RestartRequested { get; }

        public string RunningUser { get; }

        public long MemoryUsageBytes { get; }
    }
}