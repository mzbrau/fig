using Microsoft.Extensions.Options;

namespace Fig.Client.Configuration;

internal static class OptionsSingleton
{
    public static IOptionsMonitor<SettingsBase>? Options { get; set; }
}