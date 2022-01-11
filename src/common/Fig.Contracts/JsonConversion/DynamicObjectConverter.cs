using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Contracts.JsonConversion
{
    public class DynamicObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jsonObject = new JObject
            {
                ["type"] = value.GetType().AssemblyQualifiedName,
                ["value"] = JToken.FromObject(value, serializer)
            };
            jsonObject.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) 
                return null;

            var jsonObject = JObject.Load(reader);
            var type = Type.GetType(jsonObject["type"].ToString(), throwOnError: true);
            return jsonObject["value"].ToObject(type, serializer);
        }
    }
}