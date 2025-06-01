using System.Collections.Generic;
using Fig.Contracts.CustomActions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingsClientDefinitionDataContract
    {
        public SettingsClientDefinitionDataContract(string name,
            string description,
            string? instance,
            bool hasDisplayScripts,
            List<SettingDefinitionDataContract> settings,
            List<SettingVerificationDefinitionDataContract> verifications,
            IEnumerable<SettingDataContract> clientSettingOverrides,
            List<CustomActionDefinitionDataContract>? customActions = null)
        {
            Name = name;
            Description = description;
            Instance = instance;
            HasDisplayScripts = hasDisplayScripts;
            Settings = settings;
            Verifications = verifications;
            ClientSettingOverrides = clientSettingOverrides;
            CustomActions = customActions ?? new List<CustomActionDefinitionDataContract>();
        }

        public string Name { get; }
        
        public string Description { get; }

        public string? Instance { get; }
        
        public bool HasDisplayScripts { get; }

        public List<SettingDefinitionDataContract> Settings { get; }

        public List<SettingVerificationDefinitionDataContract> Verifications { get; }

        public List<CustomActionDefinitionDataContract> CustomActions { get; }
        
        public IEnumerable<SettingDataContract> ClientSettingOverrides { get; set; }
    }
}