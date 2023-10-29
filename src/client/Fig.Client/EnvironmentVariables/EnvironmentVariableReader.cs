using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Client.EnvironmentVariables;

internal class EnvironmentVariableReader : IEnvironmentVariableReader
{
    private readonly IDictionary _allEnvironmentVariables = Environment.GetEnvironmentVariables();

    public IEnumerable<SettingDataContract> ReadSettingOverrides(string clientName, IList<SettingDefinitionDataContract> settings)
    {
        var result = new List<SettingDataContract>();
        foreach (DictionaryEntry variable in _allEnvironmentVariables)
        {
            var match = settings.FirstOrDefault(a => $"{clientName}__{a.Name}" == variable.Key.ToString());
            if (match is not null)
            {
                result.Add(new SettingDataContract(
                    match.Name,
                    ValueDataContractFactory.CreateContract(variable.Value, match.ValueType!)));
            }
        }

        return result;
    }

    public void ApplyConfigurationOverrides(List<SettingDefinitionDataContract> settings)
    {
        foreach (DictionaryEntry variable in _allEnvironmentVariables)
        {
            var value = variable.Value?.ToString();
            if (value == "null")
                value = string.Empty;
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"{setting.Name}__Group",
                setting => setting.Group = value);
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"{setting.Name}__ValidationRegex",
                setting => setting.ValidationRegex = value);
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"{setting.Name}__ValidationExplanation",
                setting => setting.ValidationExplanation = value);
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"{setting.Name}__LookupTableKey",
                setting => setting.LookupTableKey = value);
        }
        
        void UpdateMatchingSettings(object environmentVariableKey, 
            Func<SettingDefinitionDataContract, string> expectedKey, 
            Action<SettingDefinitionDataContract> updateSetting)
        {
            var match = settings.FirstOrDefault(a => expectedKey(a) == environmentVariableKey.ToString());
            if (match is not null)
                updateSetting(match);
        }
    }
}