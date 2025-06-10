using System;
using System.Collections.Generic;
using Fig.Contracts.CustomActions;
using Fig.Contracts.Settings;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingsClientDefinitionDataContract
    {
        public SettingsClientDefinitionDataContract(string name,
            string description,
            string? instance,
            bool hasDisplayScripts,
            List<SettingDefinitionDataContract> settings,
            IEnumerable<SettingDataContract> clientSettingOverrides,
            List<CustomActionDefinitionDataContract>? customActions = null)
        {
            Name = name;
            Description = description;
            Instance = instance;
            HasDisplayScripts = hasDisplayScripts;
            Settings = settings;
            ClientSettingOverrides = clientSettingOverrides;
            CustomActions = customActions ?? new List<CustomActionDefinitionDataContract>();
        }

        public string Name { get; }
        
        public string Description { get; }

        public string? Instance { get; }
        
        public bool HasDisplayScripts { get; }

        public List<SettingDefinitionDataContract> Settings { get; }

        public List<CustomActionDefinitionDataContract> CustomActions { get; }
        
        public IEnumerable<SettingDataContract> ClientSettingOverrides { get; set; }
        
        [Obsolete("Removed in Fig 2.0")]
        public List<SettingVerificationDefinitionDataContract> Verifications { get; } = [];
    }
}