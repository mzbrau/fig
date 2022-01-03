using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingsClientDefinitionDataContract
    {
        public string Name { get; set; }
        
        public string ClientSecret { get; set; }

        public SettingQualifiersDataContract Qualifiers { get; set; }
        public List<SettingDefinitionDataContract> Settings { get; set; }
    }
}

