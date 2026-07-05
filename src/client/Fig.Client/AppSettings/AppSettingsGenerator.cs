using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Fig.Client.Contracts;
using Fig.Client.Parsers;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Client.AppSettings;

internal class AppSettingsGenerator
{
    internal const string EncryptedSuffix = "_FigEncrypted";

    private readonly IAppSettingsEncryptionProvider? _encryptionProvider;

    internal AppSettingsGenerator(IAppSettingsEncryptionProvider? encryptionProvider = null)
    {
        _encryptionProvider = encryptionProvider;
    }

    public void Generate(SettingsClientDefinitionDataContract definition, Dictionary<string, string> overrides)
    {
        var settings = GetSettingsDictionary(definition, overrides);
        var json = BuildNestedJson(settings);

        var outputJson = json.ToString(Formatting.Indented);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.fig.json");
        File.WriteAllText(filePath, outputJson);

        Console.WriteLine($"appsettings.json written to: {filePath}");

        if (definition.Settings.Any(HasSecretRequirement) &&
            (_encryptionProvider == null || !_encryptionProvider.IsSupported))
        {
            Console.WriteLine(
                _encryptionProvider == null
                    ? "WARNING: Secret settings were omitted because no encryption provider was configured. " +
                      "Add a DpapiSecretProvider (or another IClientSecretProvider that implements IAppSettingsEncryptionProvider) to FigOptions.ClientSecretProviders."
                    : "WARNING: Secret settings were omitted because the configured encryption provider is not supported on this platform. " +
                      "Run this command on a supported platform (e.g. Windows for DPAPI) to include encrypted secret values.");
        }
    }

    // Exposed internally for unit testing
    internal Dictionary<string, string?> GetSettingsDictionary(
        SettingsClientDefinitionDataContract definition,
        Dictionary<string, string> overrides)
    {
        var normalizedOverrides = new Dictionary<string, string>(overrides, StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in definition.Settings)
        {
            if (IsDataGrid(setting))
                AddDataGridEntries(setting, normalizedOverrides, result);
            else if (IsJson(setting))
                AddJsonEntries(setting, normalizedOverrides, result);
            else
                AddScalarEntry(setting, normalizedOverrides, result);
        }

        return result;
    }

    private void AddScalarEntry(
        SettingDefinitionDataContract setting,
        Dictionary<string, string> overrides,
        Dictionary<string, string?> result)
    {
        var normalizedName = NormalizeSettingName(setting.Name);
        var rawValue = GetEffectiveValue(setting, normalizedName, overrides);

        if (setting.IsSecret)
        {
            if (_encryptionProvider == null || !_encryptionProvider.IsSupported)
                return;

            result[normalizedName + EncryptedSuffix] =
                rawValue != null ? _encryptionProvider.Encrypt(rawValue) : null;
        }
        else
        {
            result[normalizedName] = rawValue;
        }
    }

    private void AddDataGridEntries(
        SettingDefinitionDataContract setting,
        Dictionary<string, string> overrides,
        Dictionary<string, string?> result)
    {
        var settingName = NormalizeSettingName(setting.Name);
        var value = ((DataGridSettingDataContract)setting.DefaultValue!)!.Value;

        if (value is null)
        {
            result[settingName] = null;
            return;
        }

        var secretColumnNames = setting.DataGridDefinition?.Columns
            .Where(c => c.IsSecret)
            .Select(c => c.Name);
        var secretColumns = secretColumnNames != null
            ? new HashSet<string>(secretColumnNames, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rowIndex = 0;
        var isBaseTypeList = value.FirstOrDefault()?.Count == 1 && value.First().First().Key == "Values";
        foreach (var row in value)
        {
            foreach (var kvp in row)
            {
                var relativePath = isBaseTypeList
                    ? rowIndex.ToString()
                    : ConfigurationPath.Combine(rowIndex.ToString(), kvp.Key);
                var path = ConfigurationPath.Combine(settingName, relativePath);

                if (kvp.Value is JArray arr)
                {
                    for (var i = 0; i < arr.Count; i++)
                    {
                        var arrayPath = ConfigurationPath.Combine(settingName,
                            ConfigurationPath.Combine(relativePath, i.ToString()));
                        var formattedValue = arr[i].ToString();
                        AddFlatEntry(overrides, result, arrayPath, kvp.Key, formattedValue, isSecret: false);
                    }
                }
                else
                {
                    var formattedValue = Convert.ToString(kvp.Value, CultureInfo.InvariantCulture);
                    var isSecretColumn = !isBaseTypeList && secretColumns.Contains(kvp.Key);
                    AddFlatEntry(overrides, result, path, kvp.Key, formattedValue, isSecretColumn);
                }
            }

            rowIndex++;
        }
    }

    private void AddJsonEntries(
        SettingDefinitionDataContract setting,
        Dictionary<string, string> overrides,
        Dictionary<string, string?> result)
    {
        var value = ((JsonSettingDataContract)setting.DefaultValue!)!.Value;
        if (value is null)
            return;

        var settingName = NormalizeSettingName(setting.Name);
        var parser = new JsonValueParser();
        foreach (var kvp in parser.ParseJsonValue(value))
        {
            var path = ConfigurationPath.Combine(settingName, kvp.Key);
            var overrideValue = GetOverrideForPath(overrides, path, kvp.Key);
            result[path] = overrideValue ?? kvp.Value;
        }
    }

    private void AddFlatEntry(
        Dictionary<string, string> overrides,
        Dictionary<string, string?> result,
        string path,
        string columnName,
        string? formattedValue,
        bool isSecret)
    {
        var rawValue = GetOverrideForPath(overrides, path, columnName) ?? formattedValue;

        if (isSecret)
        {
            if (_encryptionProvider == null || !_encryptionProvider.IsSupported)
                return;

            result[path + EncryptedSuffix] = rawValue != null ? _encryptionProvider.Encrypt(rawValue) : null;
        }
        else
        {
            result[path] = rawValue;
        }
    }

    private static bool HasSecretRequirement(SettingDefinitionDataContract setting) =>
        setting.IsSecret || setting.DataGridDefinition?.Columns.Any(c => c.IsSecret) == true;

    private static bool IsDataGrid(SettingDefinitionDataContract setting) =>
        setting.DefaultValue is DataGridSettingDataContract;

    private static bool IsJson(SettingDefinitionDataContract setting) =>
        setting.DefaultValue is JsonSettingDataContract;

    private static string NormalizeSettingName(string name) =>
        name.Replace(Constants.SettingPathSeparator, ":");

    private static string? GetEffectiveValue(
        SettingDefinitionDataContract setting,
        string normalizedName,
        Dictionary<string, string> overrides)
    {
        var settingSimpleName = normalizedName.Contains(':')
            ? normalizedName.Substring(normalizedName.LastIndexOf(':') + 1)
            : normalizedName;

        if (overrides.TryGetValue(setting.Name, out var overrideByOriginalName))
            return overrideByOriginalName;

        if (overrides.TryGetValue(normalizedName, out var overrideByNormalizedName))
            return overrideByNormalizedName;

        if (overrides.TryGetValue(settingSimpleName, out var overrideBySimpleName))
            return overrideBySimpleName;

        return FormatDefaultValue(setting.DefaultValue);
    }

    private static string? GetOverrideForPath(
        Dictionary<string, string> overrides,
        string path,
        string columnName)
    {
        if (overrides.TryGetValue(path, out var overrideByPath))
            return overrideByPath;

        if (overrides.TryGetValue(columnName, out var overrideByColumnName))
            return overrideByColumnName;

        return null;
    }

    private static string? FormatDefaultValue(SettingValueBaseDataContract? defaultValue)
    {
        if (defaultValue is null)
            return null;

        return defaultValue switch
        {
            StringSettingDataContract stringSetting => stringSetting.Value,
            BoolSettingDataContract boolSetting => boolSetting.Value.ToString(),
            DateTimeSettingDataContract dateTimeSetting => dateTimeSetting.Value?.ToString("o"),
            DoubleSettingDataContract doubleSetting => doubleSetting.Value.ToString(CultureInfo.InvariantCulture),
            LongSettingDataContract longSetting => longSetting.Value.ToString(CultureInfo.InvariantCulture),
            IntSettingDataContract intSetting => intSetting.Value.ToString(CultureInfo.InvariantCulture),
            TimeSpanSettingDataContract timeSpanSetting => timeSpanSetting.Value?.ToString(),
            _ => defaultValue.GetValue()?.ToString()
        };
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

        if (int.TryParse(parts[index + 1], out var arrayIndex))
        {
            if (parent[key] is not JArray array)
            {
                array = new JArray();
                parent[key] = array;
            }

            while (array.Count <= arrayIndex)
                array.Add(new JObject());

            if (index + 2 == parts.Length)
            {
                array[arrayIndex] = value;
            }
            else if (array[arrayIndex] is JObject arrayItem)
            {
                SetNestedValue(arrayItem, parts, index + 2, value);
            }
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