using System.Globalization;
using Newtonsoft.Json;

namespace Fig.Common.NetStandard.Json;

public static class JsonSettings
{
    private static readonly AlphabeticalPropertyOrderResolver PropertyOrderResolver = new();
    private static readonly FigSerializationBinder SerializationBinder = new();
    private static readonly FigSerializationBinder HttpSerializationBinder = new(useShortAssemblyNames: true);
    
    public static JsonSerializerSettings FigDefault { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        SerializationBinder = SerializationBinder,
        Culture = CultureInfo.InvariantCulture
    };

    /// <summary>
    /// HTTP wire format for API controllers and Fig.Web large GETs.
    /// Must use TypeNameHandling.Objects (never Auto — Auto emits $type for LINQ
    /// iterators on IEnumerable properties and breaks deserialize). Omits nulls to shrink payloads.
    /// Uses short assembly names in $type to cut parse cost on large /clients payloads.
    /// </summary>
    public static JsonSerializerSettings FigHttp { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        NullValueHandling = NullValueHandling.Ignore,
        SerializationBinder = HttpSerializationBinder,
        Culture = CultureInfo.InvariantCulture,
        Converters = { new ShortNameTypeJsonConverter() }
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