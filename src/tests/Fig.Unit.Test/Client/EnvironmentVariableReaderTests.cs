using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Client.Configuration;
using Fig.Client.EnvironmentVariables;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class EnvironmentVariableReaderTests
{
    private readonly List<string> _envVarsToClean = [];

    [TearDown]
    public void TearDown()
    {
        // Clean up all environment variables set during tests
        foreach (var envVar in _envVarsToClean)
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }
        _envVarsToClean.Clear();
    }

    private void SetEnvironmentVariable(string key, string value)
    {
        Environment.SetEnvironmentVariable(key, value);
        _envVarsToClean.Add(key);
    }

    [Test]
    public void ReadSettingOverrides_WithSimpleSettingName_ReadsFromMatchingEnvVar()
    {
        // Arrange
        var uniqueKey = $"MyStringSetting_{Guid.NewGuid():N}";
        SetEnvironmentVariable(uniqueKey, "overridden value");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueKey, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueKey, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo(uniqueKey));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("overridden value"));
    }

    [Test]
    public void ReadSettingOverrides_WithConfigSectionOverride_ReadsFromSectionPath()
    {
        // Arrange - e.g., Serilog:MinimumLevel:Default should match Serilog__MinimumLevel__Default
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var sectionPrefix = $"Serilog{uniqueId}";
        var envVarName = $"{sectionPrefix}__MinimumLevel__Default";
        SetEnvironmentVariable(envVarName, "Warning");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new("MinLogLevel", "Description", valueType: typeof(string))
        };
        
        // Configuration section uses ":" as separator, setting name override is "Default"
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { "MinLogLevel", [new($"{sectionPrefix}:MinimumLevel", "Default")]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo("MinLogLevel"));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("Warning"));
    }

    [Test]
    public void ReadSettingOverrides_WithConfigSectionOverride_NoSettingNameOverride_UsesSectionPlusSetting()
    {
        // Arrange - ConfigSection "MySection" without setting name override
        // Should match: MySection__MyStringSetting
        var uniqueSettingName = $"Setting_{Guid.NewGuid():N}";
        var uniqueSectionName = $"Section_{Guid.NewGuid():N}";
        var envVarName = $"{uniqueSectionName}__{uniqueSettingName}";
        SetEnvironmentVariable(envVarName, "from section");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [new(uniqueSectionName, null)]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo(uniqueSettingName));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("from section"));
    }

    [Test]
    public void ReadSettingOverrides_WithNestedSettingPath_ReadsFromPathBasedEnvVar()
    {
        // Arrange - For nested settings like School->Name, should match School__Name
        var uniquePrefix = $"School_{Guid.NewGuid():N}";
        var settingName = $"{uniquePrefix}->Name";
        var envVarName = $"{uniquePrefix}__Name";
        SetEnvironmentVariable(envVarName, "Test School");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(settingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { settingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo(settingName));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("Test School"));
    }

    [Test]
    public void ReadSettingOverrides_CaseInsensitive_MatchesEnvVar()
    {
        // Arrange
        var uniqueSettingName = $"MyStringSetting_{Guid.NewGuid():N}";
        SetEnvironmentVariable(uniqueSettingName.ToUpper(), "upper case value");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("upper case value"));
    }

    [Test]
    public void ReadSettingOverrides_NoMatchingEnvVar_ReturnsEmpty()
    {
        // Arrange
        var uniqueEnvVar = $"SomeOtherEnvVar_{Guid.NewGuid():N}";
        var uniqueSettingName = $"MyStringSetting_{Guid.NewGuid():N}";
        SetEnvironmentVariable(uniqueEnvVar, "irrelevant");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(0));
    }

    [Test]
    public void ReadSettingOverrides_MultipleConfigSections_MatchesAnyOfThem()
    {
        // Arrange - Setting with multiple config section overrides
        var uniqueSettingName = $"MySetting_{Guid.NewGuid():N}";
        var uniqueFirstSection = $"FirstSection_{Guid.NewGuid():N}";
        var uniqueSecondSection = $"SecondSection_{Guid.NewGuid():N}";
        var envVarName = $"{uniqueSecondSection}__CustomName";
        SetEnvironmentVariable(envVarName, "from second");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [
                    new(uniqueFirstSection, null),
                    new(uniqueSecondSection, "CustomName")
                ]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("from second"));
    }

    [Test]
    public void ReadSettingOverrides_WithIntType_ParsesValueCorrectly()
    {
        // Arrange
        var uniqueSettingName = $"MyIntSetting_{Guid.NewGuid():N}";
        SetEnvironmentVariable(uniqueSettingName, "42");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(int))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo(42));
    }

    [Test]
    public void ReadSettingOverrides_WithColonInConfigSection_ConvertsToDoubleUnderscore()
    {
        // Arrange - Serilog:MinimumLevel with setting name override "Default"
        // Environment variable should be Serilog__MinimumLevel__Default
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var envVarName = $"Serilog{uniqueId}__MinimumLevel__Default";
        SetEnvironmentVariable(envVarName, "Debug");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new("LogLevel", "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { "LogLevel", [new($"Serilog{uniqueId}:MinimumLevel", "Default")]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo("LogLevel"));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("Debug"));
    }

    [Test]
    public void ReadSettingOverrides_WithNullSectionAndSettingNameOverride_UsesOnlyNameOverride()
    {
        // Arrange - ConfigurationSectionOverride(null, "FIG_API_URI") should match FIG_API_URI directly
        var uniqueSettingName = $"ApiUri_{Guid.NewGuid():N}";
        var envVarName = "FIG_API_URI";
        SetEnvironmentVariable(envVarName, "http://localhost:8080");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [new(null, "FIG_API_URI")]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo(uniqueSettingName));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("http://localhost:8080"));
    }

    [Test]
    public void ReadSettingOverrides_WithEmptySectionAndSettingNameOverride_UsesOnlyNameOverride()
    {
        // Arrange - ConfigurationSectionOverride("", "CUSTOM_NAME") should match CUSTOM_NAME directly
        var uniqueSettingName = $"CustomSetting_{Guid.NewGuid():N}";
        var envVarName = "CUSTOM_NAME";
        SetEnvironmentVariable(envVarName, "custom value");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [new(string.Empty, "CUSTOM_NAME")]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo(uniqueSettingName));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("custom value"));
    }

    [Test]
    public void ReadSettingOverrides_WithNullSectionAndSettingNameOverride_CaseInsensitive()
    {
        // Arrange - Test case insensitivity with null section override
        var uniqueSettingName = $"Setting_{Guid.NewGuid():N}";
        var envVarName = "MY_OVERRIDE_NAME";
        SetEnvironmentVariable(envVarName, "test value");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [new(null, "my_override_name")]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("test value"));
    }

    [Test]
    public void ReadSettingOverrides_WithNullSectionAndSettingNameOverride_MultipleSettings()
    {
        // Arrange - Multiple settings with null section overrides
        var setting1Name = $"Setting1_{Guid.NewGuid():N}";
        var setting2Name = $"Setting2_{Guid.NewGuid():N}";
        SetEnvironmentVariable("ENV_VAR_ONE", "value one");
        SetEnvironmentVariable("ENV_VAR_TWO", "value two");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(setting1Name, "Description", valueType: typeof(string)),
            new(setting2Name, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { setting1Name, [new(null, "ENV_VAR_ONE")]
            },
            { setting2Name, [new(null, "ENV_VAR_TWO")]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(2));
        Assert.That(overrides.First(o => o.Name == setting1Name).Value?.GetValue(), Is.EqualTo("value one"));
        Assert.That(overrides.First(o => o.Name == setting2Name).Value?.GetValue(), Is.EqualTo("value two"));
    }

    [Test]
    public void ReadSettingOverrides_WithNullSectionAndSettingNameOverride_MatchesOverrideName()
    {
        // Arrange - When an override name is specified, it should match that environment variable
        var uniqueSettingName = $"MySetting_{Guid.NewGuid():N}";
        var overrideEnvVar = $"OVERRIDE_NAME_{Guid.NewGuid():N}";
        SetEnvironmentVariable(overrideEnvVar, "override value");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [new(null, overrideEnvVar)]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("override value"));
    }

    [Test]
    public void ReadSettingOverrides_WithNullSectionAndSettingNameOverride_IntType()
    {
        // Arrange - Test null section override with non-string type
        var uniqueSettingName = $"IntSetting_{Guid.NewGuid():N}";
        SetEnvironmentVariable("INT_OVERRIDE", "12345");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(int))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [new(null, "INT_OVERRIDE")]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo(12345));
    }

    [Test]
    public void ReadSettingOverrides_WithNullSectionAndSettingNameOverride_NoMatchingEnvVar()
    {
        // Arrange - Override name doesn't match any environment variable
        var uniqueSettingName = $"Setting_{Guid.NewGuid():N}";
        SetEnvironmentVariable("DIFFERENT_VAR", "value");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [new(null, "NONEXISTENT_OVERRIDE")]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(0));
    }

    [Test]
    public void ReadSettingOverrides_WithMixedConfigSections_NullSectionAndNonNullSection()
    {
        // Arrange - Multiple config sections, one with null section and one with non-null section
        // Both env vars exist, should match any of them (behavior is to match first found)
        var uniqueSettingName = $"Setting_{Guid.NewGuid():N}";
        var sectionName = $"Section_{Guid.NewGuid():N}";
        SetEnvironmentVariable("OVERRIDE_NAME", "from override");
        SetEnvironmentVariable($"{sectionName}__NestedName", "from section");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [
                    new(null, "OVERRIDE_NAME"),
                    new(sectionName, "NestedName")
                ]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert - Should match one of the environment variables
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        // Either value is acceptable since both env vars match different config sections
        Assert.That(overrides[0].Value?.GetValue(), 
            Is.EqualTo("from override").Or.EqualTo("from section"),
            "Should match one of the configured environment variables");
    }

    [Test]
    public void ReadSettingOverrides_WithNullSectionAndNullSettingNameOverride_UsesDefaultName()
    {
        // Arrange - Both section and setting name override are null, should use default behavior
        var uniqueSettingName = $"DefaultSetting_{Guid.NewGuid():N}";
        SetEnvironmentVariable(uniqueSettingName, "default behavior");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", valueType: typeof(string))
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [new(null, null)]
            }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Value?.GetValue(), Is.EqualTo("default behavior"));
    }

    #region List and Complex Object Tests

    [Test]
    public void ReadSettingOverrides_WithStringList_ReadsIndexedEnvVars()
    {
        // Arrange - Test List<string> with indexed environment variables: StringList__0, StringList__1
        var uniqueSettingName = $"StringList_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__0", "Item1");
        SetEnvironmentVariable($"{uniqueSettingName}__1", "Item2");
        SetEnvironmentVariable($"{uniqueSettingName}__2", "Item3");
        var reader = new EnvironmentVariableReader();
        
        // Note: Column name is "Values" (plural) as defined in SettingDefinitionFactory for List<string>
        var dataGridDefinition = new DataGridDefinitionDataContract(
            [new("Values", typeof(string))], 
            false);
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(List<string>),
                dataGridDefinition: dataGridDefinition)
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo(uniqueSettingName));
        
        var value = overrides[0].Value as DataGridSettingDataContract;
        Assert.That(value, Is.Not.Null);
        Assert.That(value!.Value, Is.Not.Null);
        Assert.That(value.Value!.Count, Is.EqualTo(3));
        Assert.That(value.Value[0]["Values"], Is.EqualTo("Item1"));
        Assert.That(value.Value[1]["Values"], Is.EqualTo("Item2"));
        Assert.That(value.Value[2]["Values"], Is.EqualTo("Item3"));
    }

    [Test]
    public void ReadSettingOverrides_WithComplexList_ReadsIndexedPropertyEnvVars()
    {
        // Arrange - Test List<ComplexObject> with: ComplexList__0__StringVal, ComplexList__0__IntVal
        var uniqueSettingName = $"ComplexList_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__0__StringVal", "First");
        SetEnvironmentVariable($"{uniqueSettingName}__0__IntVal", "100");
        SetEnvironmentVariable($"{uniqueSettingName}__1__StringVal", "Second");
        SetEnvironmentVariable($"{uniqueSettingName}__1__IntVal", "200");
        var reader = new EnvironmentVariableReader();
        
        var dataGridDefinition = new DataGridDefinitionDataContract(
            [
                new("StringVal", typeof(string)),
                new("IntVal", typeof(int))
            ], 
            false);
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(List<Dictionary<string, object?>>),
                dataGridDefinition: dataGridDefinition)
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        
        var value = overrides[0].Value as DataGridSettingDataContract;
        Assert.That(value, Is.Not.Null);
        Assert.That(value!.Value, Is.Not.Null);
        Assert.That(value.Value!.Count, Is.EqualTo(2));
        
        Assert.That(value.Value[0]["StringVal"], Is.EqualTo("First"));
        Assert.That(value.Value[0]["IntVal"], Is.EqualTo(100));
        Assert.That(value.Value[1]["StringVal"], Is.EqualTo("Second"));
        Assert.That(value.Value[1]["IntVal"], Is.EqualTo(200));
    }

    [Test]
    public void ReadSettingOverrides_WithComplexList_CaseInsensitivePropertyNames()
    {
        // Arrange - Test that property names are matched case-insensitively
        var uniqueSettingName = $"ComplexList_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__0__STRINGVAL", "Upper");
        SetEnvironmentVariable($"{uniqueSettingName}__0__intval", "123");
        var reader = new EnvironmentVariableReader();
        
        var dataGridDefinition = new DataGridDefinitionDataContract(
            [
                new("StringVal", typeof(string)),
                new("IntVal", typeof(int))
            ], 
            false);
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(List<Dictionary<string, object?>>),
                dataGridDefinition: dataGridDefinition)
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        var value = overrides[0].Value as DataGridSettingDataContract;
        Assert.That(value!.Value![0]["StringVal"], Is.EqualTo("Upper"));
        Assert.That(value.Value[0]["IntVal"], Is.EqualTo(123));
    }

    [Test]
    public void ReadSettingOverrides_WithComplexList_PreservesColumnCasing()
    {
        // Arrange - Test that the output uses the column name from the definition (not env var casing)
        var uniqueSettingName = $"ComplexList_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__0__stringval", "Test");
        var reader = new EnvironmentVariableReader();
        
        var dataGridDefinition = new DataGridDefinitionDataContract(
            [new("StringVal", typeof(string))], 
            false);
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(List<Dictionary<string, object?>>),
                dataGridDefinition: dataGridDefinition)
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var value = result.First().Value as DataGridSettingDataContract;
        Assert.That(value!.Value![0].ContainsKey("StringVal"), Is.True, 
            "Should preserve column casing from definition");
    }

    [Test]
    public void ReadSettingOverrides_WithNonSequentialIndices_SortsCorrectly()
    {
        // Arrange - Test that non-sequential indices (e.g., 0, 2, 5) are handled correctly
        var uniqueSettingName = $"StringList_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__2", "Third");
        SetEnvironmentVariable($"{uniqueSettingName}__0", "First");
        SetEnvironmentVariable($"{uniqueSettingName}__5", "Sixth");
        var reader = new EnvironmentVariableReader();
        
        // Note: Column name is "Values" (plural) as defined in SettingDefinitionFactory for List<string>
        var dataGridDefinition = new DataGridDefinitionDataContract(
            [new("Values", typeof(string))], 
            false);
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(List<string>),
                dataGridDefinition: dataGridDefinition)
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var value = result.First().Value as DataGridSettingDataContract;
        Assert.That(value!.Value!.Count, Is.EqualTo(3));
        Assert.That(value.Value[0]["Values"], Is.EqualTo("First")); // Index 0
        Assert.That(value.Value[1]["Values"], Is.EqualTo("Third")); // Index 2
        Assert.That(value.Value[2]["Values"], Is.EqualTo("Sixth")); // Index 5
    }

    [Test]
    public void ReadSettingOverrides_WithNoMatchingListEnvVars_ReturnsNull()
    {
        // Arrange - No env vars match the list pattern
        var uniqueSettingName = $"StringList_{Guid.NewGuid():N}";
        var otherEnvVar = $"OtherSetting_{Guid.NewGuid():N}";
        SetEnvironmentVariable(otherEnvVar, "Irrelevant");
        var reader = new EnvironmentVariableReader();
        
        // Note: Column name is "Values" (plural) as defined in SettingDefinitionFactory for List<string>
        var dataGridDefinition = new DataGridDefinitionDataContract(
            [new("Values", typeof(string))], 
            false);
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(List<string>),
                dataGridDefinition: dataGridDefinition)
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(0), "Should not return override when no env vars match");
    }

    [Test]
    public void ReadSettingOverrides_WithComplexList_HandlesDoubleProperty()
    {
        // Arrange - Test that double values are correctly parsed
        var uniqueSettingName = $"ComplexList_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__0__DoubleVal", "3.14159");
        var reader = new EnvironmentVariableReader();
        
        var dataGridDefinition = new DataGridDefinitionDataContract(
            [new("DoubleVal", typeof(double))], 
            false);
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(List<Dictionary<string, object?>>),
                dataGridDefinition: dataGridDefinition)
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var value = result.First().Value as DataGridSettingDataContract;
        Assert.That(value!.Value![0]["DoubleVal"], Is.EqualTo(3.14159).Within(0.00001));
    }

    [Test]
    public void ReadSettingOverrides_WithComplexList_HandlesBoolProperty()
    {
        // Arrange - Test that bool values are correctly parsed
        var uniqueSettingName = $"ComplexList_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__0__IsActive", "true");
        SetEnvironmentVariable($"{uniqueSettingName}__1__IsActive", "false");
        var reader = new EnvironmentVariableReader();
        
        var dataGridDefinition = new DataGridDefinitionDataContract(
            [new("IsActive", typeof(bool))], 
            false);
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(List<Dictionary<string, object?>>),
                dataGridDefinition: dataGridDefinition)
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var value = result.First().Value as DataGridSettingDataContract;
        Assert.That(value!.Value![0]["IsActive"], Is.EqualTo(true));
        Assert.That(value.Value[1]["IsActive"], Is.EqualTo(false));
    }

    [Test]
    public void ReadSettingOverrides_WithJsonComplexObject_SerializesToJson()
    {
        // Arrange - Test single complex object with: ComplexObject__StringValue, ComplexObject__IntValue
        var uniqueSettingName = $"ComplexObject_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__StringValue", "TestString");
        SetEnvironmentVariable($"{uniqueSettingName}__IntValue", "42");
        var reader = new EnvironmentVariableReader();
        
        // Single complex objects have a JsonSchema and use StringSettingDataContract
        // Note: valueType is typeof(object) since we don't have the actual type, 
        // so values will be serialized as strings
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(object),
                jsonSchema: "{ \"type\": \"object\" }")
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(1));
        Assert.That(overrides[0].Name, Is.EqualTo(uniqueSettingName));
        
        var value = overrides[0].Value as StringSettingDataContract;
        Assert.That(value, Is.Not.Null);
        // Values are strings since we don't have the actual type definition
        Assert.That(value!.Value, Does.Contain("\"StringValue\":\"TestString\""));
        Assert.That(value.Value, Does.Contain("\"IntValue\":\"42\""));
    }

    [Test]
    public void ReadSettingOverrides_WithJsonComplexObject_CaseInsensitivePropertyNames()
    {
        // Arrange - Test that property names are matched case-insensitively
        var uniqueSettingName = $"ComplexObject_{Guid.NewGuid():N}";
        SetEnvironmentVariable($"{uniqueSettingName}__STRINGVALUE", "Upper");
        SetEnvironmentVariable($"{uniqueSettingName}__intvalue", "123");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(object),
                jsonSchema: "{ \"type\": \"object\" }")
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var value = result.First().Value as StringSettingDataContract;
        Assert.That(value, Is.Not.Null);
        // Both properties should be in the JSON (stored with env var casing since no type available)
        Assert.That(value!.Value, Does.Contain("Upper"));
        Assert.That(value.Value, Does.Contain("123"));
    }

    [Test]
    public void ReadSettingOverrides_WithJsonComplexObject_NoMatchingEnvVars_ReturnsEmpty()
    {
        // Arrange - No env vars match the complex object pattern
        var uniqueSettingName = $"ComplexObject_{Guid.NewGuid():N}";
        var otherEnvVar = $"OtherSetting_{Guid.NewGuid():N}";
        SetEnvironmentVariable(otherEnvVar, "Irrelevant");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new(uniqueSettingName, "Description", 
                valueType: typeof(object),
                jsonSchema: "{ \"type\": \"object\" }")
        };
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { uniqueSettingName, [] }
        };

        // Act
        var result = reader.ReadSettingOverrides("TestClient", settings, configSections);

        // Assert
        var overrides = result.ToList();
        Assert.That(overrides.Count, Is.EqualTo(0), "Should not return override when no env vars match");
    }

    #endregion
}
