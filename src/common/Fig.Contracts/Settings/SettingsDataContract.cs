using System.Collections.Generic;

namespace Fig.Contracts.Settings
{
    public class SettingsDataContract
    {
        public string ServiceName { get; set; }
        
        public List<SettingDataContract> Settings { get; set; }
    }
}