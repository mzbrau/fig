using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fig.Common.NetStandard.Data;
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
            var value = Convert.ToString(variable.Value, CultureInfo.InvariantCulture);
            if (value == "null")
                value = string.Empty;
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"FIG_{setting.Name.ToUpper()}_GROUP",
                setting => setting.Group = value);
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"FIG_{setting.Name.ToUpper()}_VALIDATIONREGEX",
                setting => setting.ValidationRegex = value);
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"FIG_{setting.Name.ToUpper()}_VALIDATIONEXPLANATION",
                setting => setting.ValidationExplanation = value);
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"FIG_{setting.Name.ToUpper()}_LOOKUPTABLEKEY",
                setting => setting.LookupTableKey = value);
            
            UpdateMatchingSettings(variable.Key, 
                setting => $"FIG_{setting.Name.ToUpper()}_CLASSIFICATION",
                setting =>
                {
                    if (Enum.TryParse<Classification>(value, true, out var classification))
                    {
                        setting.Classification = classification;
                    }
                });
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