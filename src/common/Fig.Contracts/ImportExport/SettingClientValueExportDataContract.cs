using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class SettingClientValueExportDataContract
    {
        public SettingClientValueExportDataContract(string name, string? instance, List<SettingValueExportDataContract> settings)
        {
            Name = name;
            Instance = instance;
            Settings = settings;
        }

        public string Name { get; }

        public string? Instance { get; }

        public List<SettingValueExportDataContract> Settings { get; }
    }
}