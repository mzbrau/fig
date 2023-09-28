using Newtonsoft.Json;

namespace Fig.Contracts.ImportExport
{
    public class SettingValueExportDataContract
    {
        public SettingValueExportDataContract(
            string name, 
            object? value,
            bool isEncrypted)
        {
            Name = name;
            Value = value;
            IsEncrypted = isEncrypted;
        }
        
        public string Name { get; }

        public object? Value { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsEncrypted { get; set; }
    }
}