using Newtonsoft.Json;

namespace Fig.Common.NetStandard.Json;

public static class JsonSettings
{
    public static JsonSerializerSettings FigDefault { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.Objects
    };
}