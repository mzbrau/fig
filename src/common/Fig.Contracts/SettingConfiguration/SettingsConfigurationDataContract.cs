using System.Collections.Generic;

namespace Fig.Contracts.SettingConfiguration
{
    public class SettingsConfigurationDataContract
    {
        public string ServiceName { get; set; }

        public List<SettingConfigurationDataContract> Settings { get; set; }
    }
}