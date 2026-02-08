using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fig.Contracts.Settings
{
    public class ClientSettingsLastChangedDataContract
    {
        [JsonConstructor]
        public ClientSettingsLastChangedDataContract(
            string name,
            string? instance,
            List<SettingValueDataContract> settings)
        {
            Name = name;
            Instance = instance;
            Settings = settings;
        }

        public string Name { get; }

        public string? Instance { get; }

        public List<SettingValueDataContract> Settings { get; }
    }
}
