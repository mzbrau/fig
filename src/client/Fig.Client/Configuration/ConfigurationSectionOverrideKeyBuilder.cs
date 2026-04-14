using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Fig.Client.Configuration;

internal static class ConfigurationSectionOverrideKeyBuilder
{
    public static IEnumerable<KeyValuePair<string, string?>> BuildEntriesForFlattenedValue(
        string flattenedKey,
        string? value,
        Dictionary<string, List<CustomConfigurationSection>> configurationSections)
    {
        if (!TryResolveSettingKey(flattenedKey, configurationSections, out var settingKey, out var relativePath, out var sections))
        {
            yield break;
        }

        foreach (var entry in BuildEntriesForSettingValue(settingKey, relativePath, sections, value))
        {
            yield return entry;
        }
    }

    public static IEnumerable<KeyValuePair<string, string?>> BuildEntriesForSettingValue(
        string settingKey,
        string? relativePath,
        IEnumerable<CustomConfigurationSection> sections,
        string? value)
    {
        var leafSettingName = settingKey.Split(':').Last();

        foreach (var section in sections)
        {
            var sectionSettingName = string.IsNullOrWhiteSpace(section.SettingNameOverride)
                ? leafSettingName
                : section.SettingNameOverride!;

            var key = string.IsNullOrWhiteSpace(section.SectionName)
                ? sectionSettingName
                : ConfigurationPath.Combine(section.SectionName, sectionSettingName);

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                key = ConfigurationPath.Combine(key, relativePath);
            }

            yield return new KeyValuePair<string, string?>(key, value);
        }
    }

    private static bool TryResolveSettingKey(
        string flattenedKey,
        Dictionary<string, List<CustomConfigurationSection>> configurationSections,
        out string settingKey,
        out string? relativePath,
        out List<CustomConfigurationSection> sections)
    {
        var candidate = flattenedKey;

        while (true)
        {
            if (configurationSections.TryGetValue(candidate, out sections))
            {
                settingKey = candidate;
                relativePath = candidate.Length == flattenedKey.Length
                    ? null
                    : flattenedKey.Substring(candidate.Length + 1);
                return true;
            }

            var separatorIndex = candidate.LastIndexOf(':');
            if (separatorIndex < 0)
            {
                settingKey = string.Empty;
                relativePath = null;
                sections = [];
                return false;
            }

            candidate = candidate.Substring(0, separatorIndex);
        }
    }
}
