using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fig.Client.Abstractions.Data;
using Fig.Client.Configuration;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Client.EnvironmentVariables;

internal class EnvironmentVariableReader : IEnvironmentVariableReader
{
    private const string EnvironmentVariableDelimiter = "__";
    private readonly IDictionary _allEnvironmentVariables = Environment.GetEnvironmentVariables();

    public IEnumerable<SettingDataContract> ReadSettingOverrides(
        string clientName,
        IList<SettingDefinitionDataContract> settings,
        Dictionary<string, List<CustomConfigurationSection>> configurationSections)
    {
        var result = new List<SettingDataContract>();
        
        foreach (var setting in settings)
        {
            var envVarNames = GetEnvironmentVariableNames(setting.Name, configurationSections);
            
            foreach (DictionaryEntry variable in _allEnvironmentVariables)
            {
                var variableKey = variable.Key.ToString();
                if (envVarNames.Any(name => string.Equals(name, variableKey, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(new SettingDataContract(
                        setting.Name,
                        ValueDataContractFactory.CreateContract(variable.Value, setting.ValueType!), 
                        setting.IsSecret));
                    break; // Only add once even if multiple env vars match
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the list of environment variable names that could override a setting value.
    /// This includes the setting name itself and any configuration section override paths,
    /// formatted using the standard .NET environment variable convention (using __ as delimiter).
    /// </summary>
    private List<string> GetEnvironmentVariableNames(
        string settingName,
        Dictionary<string, List<CustomConfigurationSection>> configurationSections)
    {
        var envVarNames = new List<string>();
        
        // Extract the simplified setting name (last part after path separator)
        var simplifiedName = settingName.Split(new[] { Constants.SettingPathSeparator }, StringSplitOptions.RemoveEmptyEntries).Last();
        
        // Add the simple setting name (for flat configuration)
        envVarNames.Add(simplifiedName);
        
        // If the setting has a path structure (nested settings), add the path-based name
        if (settingName.Contains(Constants.SettingPathSeparator))
        {
            var pathBasedName = settingName.Replace(Constants.SettingPathSeparator, EnvironmentVariableDelimiter);
            envVarNames.Add(pathBasedName);
        }
        
        // Add configuration section override paths
        if (configurationSections.TryGetValue(settingName, out var sections) && sections != null)
        {
            foreach (var section in sections)
            {
                if (!string.IsNullOrEmpty(section.SectionName))
                {
                    // Use the setting name override if provided, otherwise use the simplified name
                    var settingNameInSection = section.SettingNameOverride ?? simplifiedName;
                    
                    // Build the environment variable name using __ as delimiter
                    // e.g., "Serilog:MinimumLevel" + "Default" => "Serilog__MinimumLevel__Default"
                    var sectionPath = section.SectionName.Replace(":", EnvironmentVariableDelimiter);
                    var envVarName = $"{sectionPath}{EnvironmentVariableDelimiter}{settingNameInSection}";
                    envVarNames.Add(envVarName);
                }
            }
        }
        
        return envVarNames;
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