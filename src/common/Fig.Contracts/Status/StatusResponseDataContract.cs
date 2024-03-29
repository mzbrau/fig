using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class StatusResponseDataContract
    {
        public bool SettingUpdateAvailable { get; set; }

        public double? PollIntervalMs { get; set; }

        public bool AllowOfflineSettings { get; set; }

        public bool RestartRequested { get; set; }

        public List<string>? ChangedSettings { get; set; }
    }
}