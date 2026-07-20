using System;
using System.Collections.Generic;
using Fig.Contracts.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Contracts.Json;

/// <summary>
/// Polymorphic JSON for <see cref="SettingValueBaseDataContract"/> without Newtonsoft <c>$type</c>.
/// Wire shape: <c>{"t":"s","v":"hello"}</c> where <c>t</c> is a short discriminator.
/// Used only by <see cref="FigWebLoadJsonSettings"/> (GET /clients).
/// </summary>
public sealed class SettingValueCompactJsonConverter : JsonConverter<SettingValueBaseDataContract>
{
    private const string DiscriminatorProperty = "t";
    private const string ValueProperty = "v";

    public override void WriteJson(JsonWriter writer, SettingValueBaseDataContract? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName(DiscriminatorProperty);
        writer.WriteValue(GetDiscriminator(value));
        writer.WritePropertyName(ValueProperty);
        serializer.Serialize(writer, value.GetValue());
        writer.WriteEndObject();
    }

    public override SettingValueBaseDataContract? ReadJson(
        JsonReader reader,
        Type objectType,
        SettingValueBaseDataContract? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var obj = JObject.Load(reader);
        var discriminator = obj[DiscriminatorProperty]?.Value<string>();
        if (string.IsNullOrEmpty(discriminator))
            throw new JsonSerializationException(
                "Setting value is missing compact type discriminator 't'. " +
                "GET /clients payloads must use FigWebLoadJsonSettings.");

        var valueToken = obj[ValueProperty];
        return discriminator switch
        {
            "s" => new StringSettingDataContract(valueToken?.ToObject<string>(serializer)),
            "i" => new IntSettingDataContract(valueToken?.ToObject<int?>(serializer) ?? 0),
            "b" => new BoolSettingDataContract(valueToken?.ToObject<bool?>(serializer) ?? false),
            "l" => new LongSettingDataContract(valueToken?.ToObject<long?>(serializer) ?? 0L),
            "d" => new DoubleSettingDataContract(valueToken?.ToObject<double?>(serializer) ?? 0d),
            "dt" => new DateTimeSettingDataContract(valueToken?.ToObject<DateTime?>(serializer)),
            "ts" => new TimeSpanSettingDataContract(valueToken?.ToObject<TimeSpan?>(serializer)),
            "j" => new JsonSettingDataContract(valueToken?.ToObject<string>(serializer)),
            "dg" => new DataGridSettingDataContract(
                valueToken?.ToObject<List<Dictionary<string, object?>>>(serializer)),
            _ => throw new JsonSerializationException(
                $"Unknown setting value discriminator '{discriminator}'.")
        };
    }

    private static string GetDiscriminator(SettingValueBaseDataContract value) => value switch
    {
        StringSettingDataContract => "s",
        IntSettingDataContract => "i",
        BoolSettingDataContract => "b",
        LongSettingDataContract => "l",
        DoubleSettingDataContract => "d",
        DateTimeSettingDataContract => "dt",
        TimeSpanSettingDataContract => "ts",
        JsonSettingDataContract => "j",
        DataGridSettingDataContract => "dg",
        _ => throw new JsonSerializationException(
            $"Unsupported setting value type for FigWebLoad: {value.GetType().FullName}")
    };
}
