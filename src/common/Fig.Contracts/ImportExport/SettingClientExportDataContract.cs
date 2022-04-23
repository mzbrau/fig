using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class SettingClientExportDataContract
    {
        public string Name { get; set; }

        public string ClientSecret { get; set; } = string.Empty;

        public string? Instance { get; set; }

        public List<SettingExportDataContract> Settings { get; set; }

        public List<PluginVerificationExportDataContract> PluginVerifications { get; set; }

        public List<DynamicVerificationExportDataContract> DynamicVerifications { get; set; }
    }
}