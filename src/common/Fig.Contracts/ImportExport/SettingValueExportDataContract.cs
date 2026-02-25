using Newtonsoft.Json;

namespace Fig.Contracts.ImportExport
{
    public class SettingValueExportDataContract
    {
        public SettingValueExportDataContract(
            string name, 
            object? value,
            bool isEncrypted,
            bool? isExternallyManaged,
            bool? initOnlyExport = null)
        {
            Name = name;
            Value = value;
            IsEncrypted = isEncrypted;
            IsExternallyManaged = isExternallyManaged;
            InitOnlyExport = initOnlyExport;
        }
        
        public string Name { get; }

        public object? Value { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsEncrypted { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsExternallyManaged { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? InitOnlyExport { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SettingLastChangedDataContract? LastChangedDetails { get; set; }
    }
}
