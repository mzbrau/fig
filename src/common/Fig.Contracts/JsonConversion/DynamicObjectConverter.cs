using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Contracts.JsonConversion
{
    public class DynamicObjectConverter : JsonConverter
    {
        private const string TypeProperty = "type";
        private const string ValueProperty = "value";

        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var jsonObject = new JObject
            {
                [TypeProperty] = value?.GetType().AssemblyQualifiedName,
                [ValueProperty] = value != null ? JToken.FromObject(value, serializer) : null
            };
            jsonObject.WriteTo(writer);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonObject = JObject.Load(reader);

            if (!jsonObject.ContainsKey(TypeProperty) || !jsonObject.ContainsKey(ValueProperty))
                return null;

            var typeName = jsonObject[TypeProperty];
            var value = jsonObject[ValueProperty];

            if (typeName == null || value == null)
                return null;

            var type = Type.GetType(typeName.ToString(), true);
            return value.ToObject(type!, serializer);
        }
    }
}