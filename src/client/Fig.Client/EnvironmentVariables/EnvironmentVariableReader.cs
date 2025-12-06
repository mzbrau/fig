using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Fig.Client.Abstractions.Data;
using Fig.Client.Configuration;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Newtonsoft.Json;

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
            var settingValue = TryGetSettingValue(setting, envVarNames);
            
            if (settingValue != null)
            {
                result.Add(new SettingDataContract(setting.Name, settingValue, setting.IsSecret));
            }
        }

        return result;
    }

    /// <summary>
    /// Attempts to get a setting value from environment variables.
    /// Handles simple types, lists (e.g., StringList__0, StringList__1), and 
    /// complex objects (e.g., ComplexObject__StringVal, ComplexObject__IntVal).
    /// </summary>
    private SettingValueBaseDataContract? TryGetSettingValue(
        SettingDefinitionDataContract setting,
        List<string> envVarNames)
    {
        // First, check for exact match (simple types)
        foreach (DictionaryEntry variable in _allEnvironmentVariables)
        {
            var variableKey = variable.Key.ToString();
            if (envVarNames.Any(name => string.Equals(name, variableKey, StringComparison.OrdinalIgnoreCase)))
            {
                return ValueDataContractFactory.CreateContract(variable.Value, setting.ValueType!);
            }
        }

        // Check if this is a data grid type (List<T> where T is a class or List<string>)
        if (setting.ValueType?.IsSupportedDataGridType() == true)
        {
            return TryGetDataGridValue(setting, envVarNames);
        }

        // Check if this is a JSON type (single complex object with JsonSchema)
        if (!string.IsNullOrEmpty(setting.JsonSchema))
        {
            return TryGetJsonValue(setting, envVarNames);
        }

        return null;
    }

    /// <summary>
    /// Tries to construct a DataGrid value from indexed environment variables.
    /// Supports two patterns:
    /// 1. Simple list: SettingName__0, SettingName__1, etc. for List&lt;string&gt;
    /// 2. Complex list: SettingName__0__PropertyName, SettingName__1__PropertyName for List&lt;T&gt;
    /// </summary>
    private SettingValueBaseDataContract? TryGetDataGridValue(
        SettingDefinitionDataContract setting,
        List<string> envVarNames)
    {
        var dataGridDefinition = setting.DataGridDefinition;
        if (dataGridDefinition == null)
            return null;

        // Collect all environment variables that match any of our prefixes
        var matchingEnvVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (DictionaryEntry variable in _allEnvironmentVariables)
        {
            var variableKey = variable.Key.ToString()!;
            var variableValue = variable.Value?.ToString() ?? string.Empty;
            
            foreach (var envVarName in envVarNames)
            {
                // Check if this env var starts with our setting name followed by the delimiter
                var prefix = envVarName + EnvironmentVariableDelimiter;
                if (variableKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Store the suffix part (e.g., "0" or "0__PropertyName")
                    var suffix = variableKey.Substring(prefix.Length);
                    matchingEnvVars[suffix] = variableValue;
                    break;
                }
            }
        }

        if (!matchingEnvVars.Any())
            return null;

        // Determine if this is a simple list (List<string>) or complex list (List<T>)
        // Simple list has single column named "Values" with string type
        var columns = dataGridDefinition.Columns;
        var isSimpleList = columns.Count == 1 
            && columns[0].ValueType == typeof(string) 
            && columns[0].Name == "Values";

        if (isSimpleList)
        {
            return BuildSimpleListValue(matchingEnvVars);
        }
        else
        {
            return BuildComplexListValue(matchingEnvVars, columns);
        }
    }

    /// <summary>
    /// Builds a DataGrid value for a simple List&lt;string&gt; from indexed environment variables.
    /// Environment variables should be: SettingName__0, SettingName__1, etc.
    /// </summary>
    private DataGridSettingDataContract BuildSimpleListValue(Dictionary<string, string> matchingEnvVars)
    {
        // Parse indices and sort them
        var indexedValues = new SortedDictionary<int, string>();
        
        foreach (var kvp in matchingEnvVars)
        {
            if (int.TryParse(kvp.Key, out var index))
            {
                indexedValues[index] = kvp.Value;
            }
        }

        if (!indexedValues.Any())
            return new DataGridSettingDataContract(null);

        // Build the list of dictionaries (DataGrid format)
        // Note: Column name is "Values" (plural) for List<string> as defined in SettingDefinitionFactory
        var result = new List<Dictionary<string, object?>>();
        foreach (var kvp in indexedValues)
        {
            result.Add(new Dictionary<string, object?> { { "Values", kvp.Value } });
        }

        return new DataGridSettingDataContract(result);
    }

    /// <summary>
    /// Builds a DataGrid value for a complex List&lt;T&gt; from indexed environment variables.
    /// Environment variables should be: SettingName__0__PropertyName, SettingName__1__PropertyName, etc.
    /// </summary>
    private DataGridSettingDataContract BuildComplexListValue(
        Dictionary<string, string> matchingEnvVars,
        List<DataGridColumnDataContract> columns)
    {
        // Parse structure: index__propertyName -> value
        var indexedObjects = new SortedDictionary<int, Dictionary<string, object?>>();
        
        foreach (var kvp in matchingEnvVars)
        {
            // Split the key to get index and property name
            var parts = kvp.Key.Split(new[] { EnvironmentVariableDelimiter }, 2, StringSplitOptions.None);
            
            if (parts.Length == 2 && int.TryParse(parts[0], out var index))
            {
                var propertyName = parts[1];
                
                if (!indexedObjects.TryGetValue(index, out var objDict))
                {
                    objDict = new Dictionary<string, object?>();
                    indexedObjects[index] = objDict;
                }

                // Find the column definition to get the correct type
                var column = columns.FirstOrDefault(c => 
                    string.Equals(c.Name, propertyName, StringComparison.OrdinalIgnoreCase));
                
                if (column != null)
                {
                    // Use the actual property name from column definition (preserves casing)
                    objDict[column.Name] = ConvertValue(kvp.Value, column.ValueType);
                }
                else
                {
                    // If column not found, store as string
                    objDict[propertyName] = kvp.Value;
                }
            }
        }

        if (!indexedObjects.Any())
            return new DataGridSettingDataContract(null);

        return new DataGridSettingDataContract(indexedObjects.Values.ToList());
    }

    /// <summary>
    /// Tries to get a JSON value for a single complex object setting.
    /// Environment variables should be: SettingName__PropertyName for each property.
    /// The result is serialized to JSON and returned as a StringSettingDataContract.
    /// </summary>
    private SettingValueBaseDataContract? TryGetJsonValue(
        SettingDefinitionDataContract setting,
        List<string> envVarNames)
    {
        foreach (var envVarName in envVarNames)
        {
            var prefix = envVarName + EnvironmentVariableDelimiter;
            
            // Look for env vars with the prefix (e.g., ComplexSetting__StringValue, ComplexSetting__IntValue)
            var matchingEnvVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (DictionaryEntry envVar in _allEnvironmentVariables)
            {
                var key = envVar.Key.ToString()!;
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Get the property part after the prefix
                    var propertyPart = key.Substring(prefix.Length);
                    matchingEnvVars[propertyPart] = envVar.Value?.ToString() ?? string.Empty;
                }
            }

            if (matchingEnvVars.Any())
            {
                var jsonValue = BuildJsonValue(matchingEnvVars, setting.ValueType);
                if (jsonValue != null)
                    return jsonValue;
            }
        }

        return null;
    }

    /// <summary>
    /// Builds a JSON string from property environment variables.
    /// </summary>
    private StringSettingDataContract? BuildJsonValue(
        Dictionary<string, string> propertyEnvVars,
        Type? valueType)
    {
        var jsonObject = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var kvp in propertyEnvVars)
        {
            var propertyName = kvp.Key;
            var propertyValue = kvp.Value;
            
            // Try to find the property on the type to get its correct type
            object? convertedValue = propertyValue;
            if (valueType != null)
            {
                var property = valueType.GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
                
                if (property != null)
                {
                    propertyName = property.Name; // Use actual property name casing
                    convertedValue = ConvertValue(propertyValue, property.PropertyType);
                }
            }
            
            jsonObject[propertyName] = convertedValue;
        }

        if (!jsonObject.Any())
            return null;

        var jsonString = JsonConvert.SerializeObject(jsonObject, Formatting.None);
        return new StringSettingDataContract(jsonString);
    }

    /// <summary>
    /// Converts a string value to the specified type.
    /// </summary>
    private object? ConvertValue(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        // Handle enums
        if (underlyingType.IsEnum)
        {
            try
            {
                return Enum.Parse(underlyingType, value, true);
            }
            catch
            {
                return null;
            }
        }

        // Use TypeConverter for standard types
        var converter = TypeDescriptor.GetConverter(underlyingType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                return converter.ConvertFromInvariantString(value);
            }
            catch
            {
                return null;
            }
        }

        // Fallback to Convert.ChangeType
        try
        {
            return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return value;
        }
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