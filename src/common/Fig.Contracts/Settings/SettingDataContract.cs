using Fig.Contracts.JsonConversion;
using Newtonsoft.Json;

namespace Fig.Contracts.Settings
{
    public class SettingDataContract
    {
        public string Name { get; set; }

        [JsonConverter(typeof(DynamicObjectConverter))]
        public dynamic Value { get; set; }
    }
}