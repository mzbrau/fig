using System;
using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class StatusRequestDataContract
    {
        public StatusRequestDataContract(Guid runSessionId, double uptimeSeconds, DateTime lastSettingUpdate,
            double pollIntervalMs, bool liveReload, string figVersion, string applicationVersion,
            bool offlineSettingsEnabled, bool supportsRestart, string runningUser, long memoryUsageBytes, 
            bool hasConfigurationError, List<string> configurationErrors)
        {
            RunSessionId = runSessionId;
            UptimeSeconds = uptimeSeconds;
            LastSettingUpdate = lastSettingUpdate;
            PollIntervalMs = pollIntervalMs;
            LiveReload = liveReload;
            FigVersion = figVersion;
            ApplicationVersion = applicationVersion;
            OfflineSettingsEnabled = offlineSettingsEnabled;
            SupportsRestart = supportsRestart;
            RunningUser = runningUser;
            MemoryUsageBytes = memoryUsageBytes;
            HasConfigurationError = hasConfigurationError;
            ConfigurationErrors = configurationErrors;
        }

        public Guid RunSessionId { get; }

        public double UptimeSeconds { get; }

        public DateTime LastSettingUpdate { get; }

        public double PollIntervalMs { get; }

        public bool LiveReload { get; }

        public string FigVersion { get; }

        public string ApplicationVersion { get; }

        public bool OfflineSettingsEnabled { get; }

        public bool SupportsRestart { get; }

        public string RunningUser { get; }

        public long MemoryUsageBytes { get; }
        
        public bool HasConfigurationError { get; }
        
        public List<string> ConfigurationErrors { get; }
    }
}