using System;

namespace Fig.Contracts.Status
{
    public class StatusRequestDataContract
    {
        public Guid RunSessionId { get; set; }

        public double UptimeSeconds { get; set; }

        public DateTime LastSettingUpdate { get; set; }

        public double PollIntervalMs { get; set; }

        public bool LiveReload { get; set; }

        public string FigVersion { get; set; }

        public string ApplicationVersion { get; set; }

        public bool OfflineSettingsEnabled { get; set; }

        public bool SupportsRestart { get; set; }
    }
}