using System.Collections.Generic;

namespace Fig.Client.Configuration;

public static class ConfigurationSectionKeyNormalizer
{
    public static Dictionary<string, List<CustomConfigurationSection>> Normalize(
        Dictionary<string, List<CustomConfigurationSection>> configurationSections)
    {
        var normalized = new Dictionary<string, List<CustomConfigurationSection>>();
        foreach (var entry in configurationSections)
        {
            normalized[entry.Key.Replace(Constants.SettingPathSeparator, ":")] = entry.Value;
        }

        return normalized;
    }
}
