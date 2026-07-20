using System;
using Newtonsoft.Json;

namespace Fig.Common.NetStandard.Json;

/// <summary>
/// Serializes <see cref="Type"/> with short assembly names (no Version/Culture/PublicKeyToken)
/// so FigHttp payloads do not embed full AssemblyQualifiedName on every ValueType.
/// Deserializes both short and full assembly-qualified names.
/// </summary>
public sealed class ShortNameTypeJsonConverter : JsonConverter<Type>
{
    public override void WriteJson(JsonWriter writer, Type? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        var qualified = value.AssemblyQualifiedName
                        ?? value.FullName
                        ?? value.Name;
        writer.WriteValue(FigSerializationBinder.RemoveAssemblyDetails(qualified));
    }

    public override Type? ReadJson(
        JsonReader reader,
        Type objectType,
        Type? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.String)
            throw new JsonSerializationException(
                $"Unexpected token {reader.TokenType} when parsing System.Type.");

        var typeName = (string?)reader.Value;
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        var resolved = Type.GetType(typeName, throwOnError: false);
        if (resolved is not null)
            return resolved;

        // Fallback: binder path used for Fig types with simple assembly names.
        var comma = typeName!.IndexOf(',');
        if (comma > 0)
        {
            var typePart = typeName.Substring(0, comma).Trim();
            var assemblyPart = typeName.Substring(comma + 1).Trim();
            var assemblySimple = assemblyPart.Split(',')[0].Trim();
            resolved = Type.GetType($"{typePart}, {assemblySimple}", throwOnError: false);
            if (resolved is not null)
                return resolved;
        }

        throw new JsonSerializationException($"Could not resolve type '{typeName}'.");
    }
}
