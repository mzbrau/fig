using System.Globalization;
using Newtonsoft.Json;

namespace Fig.Common.NetStandard.Json;

public static class JsonSettings
{
    private static readonly AlphabeticalPropertyOrderResolver PropertyOrderResolver = new();
    
    public static JsonSerializerSettings FigDefault { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        Culture = CultureInfo.InvariantCulture
    };
    
    public static JsonSerializerSettings FigUserFacing { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
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