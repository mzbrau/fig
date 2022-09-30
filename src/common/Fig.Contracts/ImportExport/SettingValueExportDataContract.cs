using System;
using Fig.Contracts.JsonConversion;
using Newtonsoft.Json;

namespace Fig.Contracts.ImportExport
{
    public class SettingValueExportDataContract
    {
        public SettingValueExportDataContract(string name, dynamic value)
        {
            Name = name;
            Value = value;
        }
        
        public string Name { get; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic? Value { get; set; }
    }
}