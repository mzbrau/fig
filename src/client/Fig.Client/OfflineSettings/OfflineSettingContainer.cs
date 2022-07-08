using System;
using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Client.OfflineSettings
{
    public class OfflineSettingContainer
    {
        public OfflineSettingContainer(DateTime persistedUtc, IEnumerable<SettingDataContract> settings)
        {
            PersistedUtc = persistedUtc;
            Settings = settings;
        }

        public DateTime PersistedUtc { get; set; }

        public IEnumerable<SettingDataContract> Settings { get; set; }
    }
}