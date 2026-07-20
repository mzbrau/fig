using System.Globalization;
using Fig.Common.NetStandard.Json;
using Newtonsoft.Json;

namespace Fig.Contracts.Json;

/// <summary>
/// Compact JSON for Fig.Web / Fig.Mcp <c>GET /clients</c> only.
/// Uses <see cref="TypeNameHandling.None"/> with
/// <see cref="SettingValueCompactJsonConverter"/> so polymorphic setting values
/// round-trip without Newtonsoft <c>$type</c> / binder cost.
/// Do not use for Fig.Client register/get-settings (<see cref="JsonSettings.FigHttp"/>).
/// </summary>
public static class FigWebLoadJsonSettings
{
    public static JsonSerializerSettings Instance { get; } = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        Culture = CultureInfo.InvariantCulture,
        Converters =
        {
            new SettingValueCompactJsonConverter(),
            new ShortNameTypeJsonConverter()
        }
    };
}
