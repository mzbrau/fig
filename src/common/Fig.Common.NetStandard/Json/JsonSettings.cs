using System.Globalization;
using Newtonsoft.Json;

namespace Fig.Common.NetStandard.Json;

public static class JsonSettings
{
    private static readonly AlphabeticalPropertyOrderResolver PropertyOrderResolver = new();
    private static readonly FigSerializationBinder SerializationBinder = new();
    
    public static JsonSerializerSettings FigDefault { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        SerializationBinder = SerializationBinder,
        Culture = CultureInfo.InvariantCulture
    };
    
    public static JsonSerializerSettings FigUserFacing { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        SerializationBinder = SerializationBinder,
        Culture = CultureInfo.InvariantCulture,
        Formatting = Formatting.Indented,
        ContractResolver = PropertyOrderResolver
    };
    
    public static JsonSerializerSettings FigMinimalUserFacing { get; } = new()
    {
        Culture = CultureInfo.InvariantCulture,
        Formatting = Formatting.Indented,
        ContractResolver = PropertyOrderResolver
    };
}