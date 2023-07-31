using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class SettingClientExportDataContract
    {
        public SettingClientExportDataContract(string name, string description, string clientSecret, string? instance, List<SettingExportDataContract> settings, List<PluginVerificationExportDataContract> pluginVerifications, List<DynamicVerificationExportDataContract> dynamicVerifications)
        {
            Name = name;
            Description = description;
            ClientSecret = clientSecret;
            Instance = instance;
            Settings = settings;
            PluginVerifications = pluginVerifications;
            DynamicVerifications = dynamicVerifications;
        }

        public string Name { get; set; }
        
        public string Description { get; }

        public string ClientSecret { get; }

        public string? Instance { get; }

        public List<SettingExportDataContract> Settings { get; }

        public List<PluginVerificationExportDataContract> PluginVerifications { get; }

        public List<DynamicVerificationExportDataContract> DynamicVerifications { get; }
    }
}