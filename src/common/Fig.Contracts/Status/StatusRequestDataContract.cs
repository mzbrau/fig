using System;
using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class StatusRequestDataContract
    {
        public StatusRequestDataContract(Guid runSessionId, DateTime startTime, DateTime lastSettingUpdate,
            double pollIntervalMs, string figVersion, string applicationVersion,
            bool offlineSettingsEnabled, bool supportsRestart, string runningUser, long memoryUsageBytes, 
            bool hasConfigurationError, List<string> configurationErrors)
        {
            RunSessionId = runSessionId;
            StartTime = startTime;
            LastSettingUpdate = lastSettingUpdate;
            PollIntervalMs = pollIntervalMs;
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

        public DateTime StartTime { get; }

        public DateTime LastSettingUpdate { get; }

        public double PollIntervalMs { get; }

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