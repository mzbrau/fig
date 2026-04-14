using System;
using Fig.Contracts.Settings;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fig.Client.Configuration;
using Fig.Client.Parsers;
using Microsoft.Extensions.Configuration;
using Fig.Common.NetStandard.IpAddress;
using Newtonsoft.Json.Linq;

namespace Fig.Client.ExtensionMethods;

internal static class SettingDataContractExtensionMethods
{
    public static Dictionary<string, string?> ToDataProviderFormat(
        this List<SettingDataContract> settings, 
        IIpAddressResolver ipAddressResolver, 
        Dictionary<string, List<CustomConfigurationSection>> configurationSections)
    {
        var dictionary = new Dictionary<string, string?>();

        foreach (var setting in settings.Where(IsSimpleValue))
        {
            var settingValue = setting.Value switch
            {
                StringSettingDataContract stringSetting => stringSetting.Value.ReplaceConstants(ipAddressResolver),
                BoolSettingDataContract boolSetting => boolSetting.Value.ToString(),
                DateTimeSettingDataContract dateTimeSetting => dateTimeSetting.Value?.ToString("o"),
                DoubleSettingDataContract doubleSetting => doubleSetting.Value.ToString(CultureInfo.InvariantCulture),
                LongSettingDataContract longSetting => longSetting.Value.ToString(CultureInfo.InvariantCulture),
                IntSettingDataContract intSetting => intSetting.Value.ToString(CultureInfo.InvariantCulture),
                TimeSpanSettingDataContract timeSpanSetting => timeSpanSetting.Value?.ToString(),
                _ => null
            };

            var simplifiedName = setting.Name.Split([Constants.SettingPathSeparator], StringSplitOptions.RemoveEmptyEntries).Last();
            dictionary[simplifiedName] = settingValue;
            
            var joinedName = setting.Name.Replace(Constants.SettingPathSeparator, ":");
            dictionary[joinedName] = settingValue;
            
            // Add entries for each configuration section if they exist
            if (configurationSections.TryGetValue(setting.Name, out var sections) && sections != null)
            {
                foreach (var sectionValue in ConfigurationSectionOverrideKeyBuilder.BuildEntriesForSettingValue(joinedName, null, sections, settingValue))
                {
                    dictionary[sectionValue.Key] = sectionValue.Value;
                }
            }
        }

        foreach (var setting in settings.Where(IsDataGrid))
        {
            var value = ((DataGridSettingDataContract)setting.Value!)!.Value;
            var settingName = setting.Name.Replace(Constants.SettingPathSeparator, ":");
            var sections = new List<CustomConfigurationSection>();
            if (configurationSections.TryGetValue(setting.Name, out var configSections) && configSections != null)
            {
                sections = configSections;
            }

            if (value is null)
            {
                dictionary[settingName] = null;

                foreach (var sectionValue in ConfigurationSectionOverrideKeyBuilder.BuildEntriesForSettingValue(settingName, null, sections, null))
                {
                    dictionary[sectionValue.Key] = sectionValue.Value;
                }

                continue;
            }

            var rowIndex = 0;
            var isBaseTypeList = value.FirstOrDefault()?.Count == 1 && value.First().First().Key == "Values";
            foreach (var row in value)
            {
                foreach (var kvp in row)
                {
                    // Add the setting with default path
                    var relativePath = isBaseTypeList
                        ? rowIndex.ToString()
                        : ConfigurationPath.Combine(rowIndex.ToString(), kvp.Key);
                    var path = ConfigurationPath.Combine(settingName, relativePath);

                    if (kvp.Value is JArray arr)
                    {
                        for (var i = 0; i < arr.Count; i++)
                        {
                            var arrayRelativePath = ConfigurationPath.Combine(relativePath, i.ToString());
                            var arrayPath = ConfigurationPath.Combine(settingName, arrayRelativePath);
                            dictionary[arrayPath] = arr[i].ToString();

                            foreach (var sectionValue in ConfigurationSectionOverrideKeyBuilder.BuildEntriesForSettingValue(settingName, arrayRelativePath, sections, arr[i].ToString()))
                            {
                                dictionary[sectionValue.Key] = sectionValue.Value;
                            }
                        }
                    }
                    else
                    {
                        var formattedValue = Convert.ToString(kvp.Value, CultureInfo.InvariantCulture)?.ReplaceConstants(ipAddressResolver);
                        dictionary[path] = formattedValue;

                        foreach (var sectionValue in ConfigurationSectionOverrideKeyBuilder.BuildEntriesForSettingValue(settingName, relativePath, sections, formattedValue))
                        {
                            dictionary[sectionValue.Key] = sectionValue.Value;
                        }
                    }
                }
            
                rowIndex++;
            }
        }

        foreach (var setting in settings.Where(IsJson))
        {
            var value = ((JsonSettingDataContract)setting.Value!)!.Value;
            if (value is not null)
            {
                var parser = new JsonValueParser();
                var parsedValues = parser.ParseJsonValue(value);
                var settingName = setting.Name.Replace(Constants.SettingPathSeparator, ":");
                var sections = configurationSections.TryGetValue(setting.Name, out var configSections) && configSections != null
                    ? configSections
                    : [];

                foreach (var kvp in parsedValues)
                {
                    var formattedValue = kvp.Value.ReplaceConstants(ipAddressResolver);
                    var key = ConfigurationPath.Combine(settingName, kvp.Key);
                    dictionary[key] = formattedValue;

                    foreach (var sectionValue in ConfigurationSectionOverrideKeyBuilder.BuildEntriesForSettingValue(settingName, kvp.Key, sections, formattedValue))
                    {
                        dictionary[sectionValue.Key] = sectionValue.Value;
                    }
                }
            }
        }

        return dictionary;
    }

    private static bool IsSimpleValue(SettingDataContract settingDataContract)
    {
        return settingDataContract.Value is not DataGridSettingDataContract and not JsonSettingDataContract;
    }

    private static bool IsDataGrid(SettingDataContract settingDataContract)
    {
        return settingDataContract.Value is DataGridSettingDataContract;
    }

    private static bool IsJson(SettingDataContract settingDataContract)
    {
        return settingDataContract.Value is JsonSettingDataContract;
    }
}
