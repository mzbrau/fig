using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Contracts.SettingConfiguration
{
    public class SettingsClientConfigurationDataContract
    {
        public string ServiceName { get; set; }

        public string? Hostname { get; set; }
    
        public string? Username { get; set; }
    
        public string? Instance { get; set; }

        public List<SettingConfigurationDataContract> Settings { get; set; }
    }
}