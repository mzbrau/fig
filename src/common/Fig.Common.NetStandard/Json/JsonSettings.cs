using System.Globalization;
using Newtonsoft.Json;

namespace Fig.Common.NetStandard.Json;

public static class JsonSettings
{
    public static JsonSerializerSettings FigDefault { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        Culture = CultureInfo.InvariantCulture
    };
    
    public static JsonSerializerSettings FigUserFacing { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Objects,
        Culture = CultureInfo.InvariantCulture,
        Formatting = Formatting.Indented
    };
}