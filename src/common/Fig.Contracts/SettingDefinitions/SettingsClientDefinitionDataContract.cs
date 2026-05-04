using System;
using System.Collections.Generic;
using Fig.Contracts.CustomActions;
using Fig.Contracts.SettingMigrations;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingsClientDefinitionDataContract
    {
        public SettingsClientDefinitionDataContract(string name,
            string? description,
            string? instance,
            bool hasDisplayScripts,
            List<SettingDefinitionDataContract> settings,
            IEnumerable<SettingDataContract> clientSettingOverrides,
            List<CustomActionDefinitionDataContract>? customActions = null,
            string? clientVersion = null,
            List<SettingMigrationResultDataContract>? settingMigrationResults = null)
        {
            Name = name;
            Description = description;
            Instance = instance;
            HasDisplayScripts = hasDisplayScripts;
            Settings = settings;
            ClientSettingOverrides = clientSettingOverrides;
            CustomActions = customActions ?? new List<CustomActionDefinitionDataContract>();
            ClientVersion = clientVersion;
            SettingMigrationResults = settingMigrationResults ?? new List<SettingMigrationResultDataContract>();
        }

        public string Name { get; }
        
        public string? Description { get; }

        public string? Instance { get; }
        
        public bool HasDisplayScripts { get; }

        public List<SettingDefinitionDataContract> Settings { get; }

        public List<CustomActionDefinitionDataContract> CustomActions { get; }
        
        public IEnumerable<SettingDataContract> ClientSettingOverrides { get; set; }
        
        /// <summary>
        /// The version of the client application at the time of registration.
        /// </summary>
        public string? ClientVersion { get; }

        public List<SettingMigrationResultDataContract> SettingMigrationResults { get; set; }
        
        [Obsolete("Removed in Fig 2.0")]
        public List<SettingVerificationDefinitionDataContract> Verifications { get; } = [];
    }
}
