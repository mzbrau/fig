using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fig.Client.Abstractions.Attributes;
using Fig.Contracts.SettingDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Client.AppSettings;

internal class AppSettingsGenerator
{
    internal const string EncryptedSuffix = "_FigEncrypted";

    private readonly IDpapiValueProcessor _dpapiProcessor;

    public AppSettingsGenerator()
        : this(new DpapiValueProcessor())
    {
    }

    internal AppSettingsGenerator(IDpapiValueProcessor dpapiProcessor)
    {
        _dpapiProcessor = dpapiProcessor;
    }

    public void Generate(SettingsClientDefinitionDataContract definition, Dictionary<string, string> overrides)
    {
        var settings = GetSettingsDictionary(definition, overrides);
        var json = BuildNestedJson(settings);

        var outputJson = json.ToString(Formatting.Indented);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        File.WriteAllText(filePath, outputJson);

        Console.WriteLine($"appsettings.json written to: {filePath}");

        if (!_dpapiProcessor.IsSupported && definition.Settings.Any(s => s.IsSecret))
        {
            Console.WriteLine(
                "WARNING: Secret settings were omitted because DPAPI encryption is only supported on Windows. " +
                "Run this command on Windows to include encrypted secret values.");
        }
    }

    // Exposed internally for unit testing
    internal Dictionary<string, string?> GetSettingsDictionary(
        SettingsClientDefinitionDataContract definition,
        Dictionary<string, string> overrides)
    {
        // Normalize overrides to a case-insensitive lookup
        var normalizedOverrides = new Dictionary<string, string>(overrides, StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in definition.Settings)
        {
            var rawValue = GetEffectiveValue(setting, normalizedOverrides);

            if (setting.IsSecret)
            {
                if (!_dpapiProcessor.IsSupported)
                    continue;

                var encryptedKey = setting.Name + EncryptedSuffix;
                result[encryptedKey] = rawValue != null ? _dpapiProcessor.Encrypt(rawValue) : null;
            }
            else
            {
                result[setting.Name] = rawValue;
            }
        }

        return result;
    }

    private static string? GetEffectiveValue(SettingDefinitionDataContract setting,
        Dictionary<string, string> overrides)
    {
        // Command-line overrides take precedence, matched case-insensitively on the setting name
        var settingSimpleName = setting.Name.Contains(':')
            ? setting.Name.Substring(setting.Name.LastIndexOf(':') + 1)
            : setting.Name;

        if (overrides.TryGetValue(setting.Name, out var overrideByFullName))
            return overrideByFullName;

        if (overrides.TryGetValue(settingSimpleName, out var overrideBySimpleName))
            return overrideBySimpleName;

        return setting.DefaultValue?.GetValue()?.ToString();
    }

    private static JObject BuildNestedJson(Dictionary<string, string?> settings)
    {
        var root = new JObject();
        foreach (var setting in settings)
        {
            var parts = setting.Key.Split(':');
            SetNestedValue(root, parts, 0, setting.Value);
        }

        return root;
    }

    private static void SetNestedValue(JObject parent, string[] parts, int index, string? value)
    {
        var key = parts[index];

        if (index == parts.Length - 1)
        {
            parent[key] = value;
            return;
        }

        // If next segment is a numeric index, we're dealing with an array
        if (int.TryParse(parts[index + 1], out var arrayIndex))
        {
            if (parent[key] is not JArray array)
            {
                array = new JArray();
                parent[key] = array;
            }

            while (array.Count <= arrayIndex)
                array.Add(new JObject());

            if (array[arrayIndex] is JObject arrayItem)
                SetNestedValue(arrayItem, parts, index + 2, value);
        }
        else
        {
            if (parent[key] is not JObject nested)
            {
                nested = new JObject();
                parent[key] = nested;
            }

            SetNestedValue(nested, parts, index + 1, value);
        }
    }
}
