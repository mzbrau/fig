using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingsDefinitionDataContract
    {
        public string ServiceName { get; set; }

        public string ServiceSecret { get; set; }
        
        public List<SettingDefinitionDataContract> Settings { get; set; }
    }
}

