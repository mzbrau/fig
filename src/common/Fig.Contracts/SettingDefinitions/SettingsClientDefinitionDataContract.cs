using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingsClientDefinitionDataContract
    {
        public string Id { get; set; }
        
        public string Name { get; set; }

        public string Instance { get; set; }
        
        public List<SettingDefinitionDataContract> Settings { get; set; }
    }
}

