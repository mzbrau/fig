using System.Collections.Generic;
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
            IEnumerable<SettingDataContract> clientSettingOverrides)
        {
            Name = name;
            Description = description;
            Instance = instance;
            HasDisplayScripts = hasDisplayScripts;
            Settings = settings;
            Verifications = verifications;
            ClientSettingOverrides = clientSettingOverrides;
        }

        public string Name { get; }
        
        public string Description { get; }

        public string? Instance { get; }
        
        public bool HasDisplayScripts { get; }

        public List<SettingDefinitionDataContract> Settings { get; }

        public List<SettingVerificationDefinitionDataContract> Verifications { get; }
        
        public IEnumerable<SettingDataContract> ClientSettingOverrides { get; set; }
    }
}