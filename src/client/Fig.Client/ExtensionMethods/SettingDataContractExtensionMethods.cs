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
        Dictionary<string, CustomConfigurationSection> configurationSections)
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
                TimeSpanSettingDataContract timeSpanSetting => timeSpanSetting.Value.ToString(),
                _ => null
            };

            var simplifiedName = setting.Name.Split([Constants.SettingPathSeparator], StringSplitOptions.RemoveEmptyEntries).Last();
            
            var configurationSection = new CustomConfigurationSection();
            if (configurationSections.TryGetValue(setting.Name, out var section))
                configurationSection = section;
            
            dictionary[simplifiedName] = settingValue;
            if (!string.IsNullOrEmpty(configurationSection.SectionName))
            {
                dictionary[$"{configurationSection.SectionName}:{configurationSection.SettingNameOverride ?? simplifiedName}"] = settingValue;
            }
        }

        foreach (var setting in settings.Where(IsDataGrid))
        {
            var value = ((DataGridSettingDataContract)setting.Value!)!.Value;
            
            if (value is null)
            {
                dictionary[setting.Name] = null;
                continue;
            }
            
            var rowIndex = 0;
            var isBaseTypeList = value.FirstOrDefault()?.Count == 1 && value.First().First().Key == "Values";
            foreach (var row in value)
            {
                foreach (var kvp in row)
                {
                    var path = isBaseTypeList
                        ? ConfigurationPath.Combine(setting.Name, rowIndex.ToString())
                        : ConfigurationPath.Combine(setting.Name, rowIndex.ToString(), kvp.Key);
                    if (kvp.Value is JArray arr)
                    {
                        for (var i = 0; i < arr.Count; i++)
                        {
                            dictionary[ConfigurationPath.Combine(path, i.ToString())] = arr[i].ToString();
                        }
                    }
                    else
                    {
                        dictionary[path] = Convert.ToString(kvp.Value, CultureInfo.InvariantCulture)?.ReplaceConstants(ipAddressResolver);
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
                foreach (var kvp in parser.ParseJsonValue(value))
                {
                    var key = ConfigurationPath.Combine(setting.Name, kvp.Key);
                    dictionary[key] = kvp.Value.ReplaceConstants(ipAddressResolver);
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