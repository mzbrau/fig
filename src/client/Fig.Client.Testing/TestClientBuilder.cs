using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client.Testing;

/// <summary>
/// Fluent builder for creating test clients from SettingsBase instances
/// </summary>
/// <typeparam name="T">The settings class type</typeparam>
public class TestClientBuilder<T> where T : SettingsBase
{
    private readonly T _settingsInstance;
    private readonly string _clientName;
    private readonly DisplayScriptTestRunner _testRunner;
    private readonly Dictionary<string, object?> _valueOverrides = new();

    internal TestClientBuilder(T settingsInstance, string clientName, DisplayScriptTestRunner testRunner)
    {
        _settingsInstance = settingsInstance;
        _clientName = clientName;
        _testRunner = testRunner;
    }

    /// <summary>
    /// Override the value of a specific setting
    /// </summary>
    /// <param name="settingName">The name of the setting to override</param>
    /// <param name="value">The new value for the setting</param>
    /// <returns>The builder for method chaining</returns>
    public TestClientBuilder<T> WithSetting(string settingName, object? value)
    {
        _valueOverrides[settingName] = value;
        return this;
    }

    /// <summary>
    /// Build the test client with all configurations applied
    /// </summary>
    /// <returns>A configured test client</returns>
    public TestClient Build()
    {
        // Create the data contract from the settings instance
        var dataContract = _settingsInstance.CreateDataContract(_clientName);
        
        // Create test client
        var testClient = _testRunner.CreateTestClient(_clientName);

        // Add settings from the data contract with proper attributes
        foreach (var settingDef in dataContract.Settings)
        {
            var overrideValue = _valueOverrides.TryGetValue(settingDef.Name, out var valOverride) 
                ? valOverride 
                : GetDefaultValue(settingDef);

            AddSettingToClient(testClient, settingDef, overrideValue);
        }

        return testClient;
    }

    private object? GetDefaultValue(SettingDefinitionDataContract settingDef)
    {
        // Use the default value from the definition if available
        if (settingDef.DefaultValue != null)
        {
            return settingDef.DefaultValue.GetValue();
        }

        // Use the actual property value from the settings instance
        var property = typeof(T).GetProperty(settingDef.Name);
        if (property != null)
        {
            try
            {
                return property.GetValue(_settingsInstance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get property value for {settingDef.Name}: {ex.Message}");
                return null;
            }
        }

        // Return type default
        return settingDef.ValueType?.IsValueType == true 
            ? Activator.CreateInstance(settingDef.ValueType) 
            : null;
    }

    private void AddSettingToClient(TestClient testClient, SettingDefinitionDataContract settingDef, object? value)
    {
        var setting = CreateTestSettingFromDefinition(settingDef, value);
        testClient.AddSetting(setting);
    }

    private IScriptableSetting CreateTestSettingFromDefinition(SettingDefinitionDataContract settingDef, object? value)
    {
        var testSetting = CreateBasicTestSetting(settingDef, value);
        
        // Apply attributes from the definition
        ApplySettingAttributes(testSetting, settingDef);
        
        return testSetting;
    }

    private TestSetting CreateBasicTestSetting(SettingDefinitionDataContract settingDef, object? value)
    {
        var valueType = settingDef.ValueType ?? typeof(string);
        
        if (valueType == typeof(List<Dictionary<string, object>>))
        {
            // Handle data grid settings
            var dataGridValue = ConvertToDataGridValue(value);
            return new TestDataGridSetting(settingDef.Name, dataGridValue);
        }

        if (valueType == typeof(TimeSpan) || valueType == typeof(TimeSpan?))
        {
            var timeSpanValue = value as TimeSpan? ?? TimeSpan.Zero;
            return new TestTimeSpanSetting(settingDef.Name, timeSpanValue);
        }

        if (settingDef.ValidValues?.Any() == true)
        {
            var dropDownValue = value?.ToString() ?? settingDef.ValidValues.FirstOrDefault() ?? string.Empty;
            return new TestDropDownSetting(settingDef.Name, dropDownValue, settingDef.ValidValues);
        }

        return new TestSetting(settingDef.Name, valueType, value);
    }
    
    private void ApplySettingAttributes(TestSetting testSetting, SettingDefinitionDataContract settingDef)
    {
        // Get all writable properties from TestSetting
        var testSettingProperties = typeof(TestSetting)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        // Get all readable properties from SettingDefinitionDataContract
        var settingDefProperties = typeof(SettingDefinitionDataContract)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        // Map matching properties automatically
        foreach (var sourceProperty in settingDefProperties)
        {
            if (testSettingProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                try
                {
                    var sourceValue = sourceProperty.GetValue(settingDef);
                    
                    // Handle special cases and type conversions
                    var convertedValue = ConvertPropertyValue(sourceValue, targetProperty.PropertyType, sourceProperty.Name);
                    
                    if (convertedValue != null || IsNullableType(targetProperty.PropertyType))
                    {
                        targetProperty.SetValue(testSetting, convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to map property '{sourceProperty.Name}': {ex.Message}");
                }
            }
        }
        
        // Set IsValid based on validation
        testSetting.IsValid = ValidateSetting(testSetting, settingDef);
    }

    private object? ConvertPropertyValue(object? sourceValue, Type targetType, string propertyName)
    {
        if (sourceValue == null)
        {
            return null;
        }

        var sourceType = sourceValue.GetType();
        var actualTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Direct assignment if types match
        if (sourceType == targetType || sourceType == actualTargetType)
        {
            return sourceValue;
        }

        // String conversion for string properties
        if (actualTargetType == typeof(string))
        {
            return sourceValue.ToString();
        }

        // Handle special conversions for known incompatible types
        switch (propertyName.ToLowerInvariant())
        {
            case "validationexplanation":
            case "categorycolor":
            case "categoryname":
                // Ensure these are never null for TestSetting
                return sourceValue?.ToString() ?? string.Empty;
                
            default:
                // For other types, try direct conversion
                if (actualTargetType.IsAssignableFrom(sourceType))
                {
                    return sourceValue;
                }
                
                // Skip properties that can't be converted
                return null;
        }
    }

    private static bool IsNullableType(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    private bool ValidateSetting(TestSetting testSetting, SettingDefinitionDataContract settingDef)
    {
        if (!string.IsNullOrEmpty(settingDef.ValidationRegex))
        {
            var stringValue = testSetting.GetValue()?.ToString() ?? string.Empty;
            try
            {
                return System.Text.RegularExpressions.Regex.IsMatch(stringValue, settingDef.ValidationRegex!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid regex pattern '{settingDef.ValidationRegex}': {ex.Message}");
                return false;
            }
        }
        
        return true;
    }

    private List<Dictionary<string, IDataGridValueModel>>? ConvertToDataGridValue(object? value)
    {
        if (value == null) return null;
        
        // Handle different data grid value formats
        if (value is List<Dictionary<string, IDataGridValueModel>> typedValue)
        {
            return typedValue;
        }
        
        if (value is List<Dictionary<string, object>> objectValue)
        {
            return objectValue.Select(row => row.ToDictionary(
                kvp => kvp.Key,
                kvp => (IDataGridValueModel)new TestDataGridValueModel(kvp.Value)
            )).ToList();
        }
        
        return new List<Dictionary<string, IDataGridValueModel>>();
    }
}