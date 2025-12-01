using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Client.Configuration;
using Fig.Client.EnvironmentVariables;
using Fig.Contracts.SettingDefinitions;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class EnvironmentVariableReaderTests
{
    private readonly List<string> _envVarsToClean = new();

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
            { uniqueKey, new List<CustomConfigurationSection>() }
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
        var uniqueEnvKey = $"Serilog_{Guid.NewGuid():N}__MinimumLevel__Default";
        var sectionName = uniqueEnvKey.Split("__")[0].Replace("_", ":").Replace(":", "__").Split("__")[0];
        SetEnvironmentVariable(uniqueEnvKey, "Warning");
        var reader = new EnvironmentVariableReader();
        
        var settings = new List<SettingDefinitionDataContract>
        {
            new("MinLogLevel", "Description", valueType: typeof(string))
        };
        
        // The configuration section format uses ":" as separator
        // When converted to env var format, it becomes "__"
        var sectionPath = uniqueEnvKey.Replace("__", ":").Substring(0, uniqueEnvKey.LastIndexOf("__", StringComparison.Ordinal));
        sectionPath = sectionPath.Replace("__", ":");
        
        var configSections = new Dictionary<string, List<CustomConfigurationSection>>
        {
            { "MinLogLevel", new List<CustomConfigurationSection>
                {
                    new(sectionPath.Replace(":", ":").Substring(0, sectionPath.LastIndexOf(":", StringComparison.Ordinal)), "Default")
                }
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
            { uniqueSettingName, new List<CustomConfigurationSection>
                {
                    new(uniqueSectionName, null) // No setting name override
                }
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
            { settingName, new List<CustomConfigurationSection>() }
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
            { uniqueSettingName, new List<CustomConfigurationSection>() }
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
            { uniqueSettingName, new List<CustomConfigurationSection>() }
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
            { uniqueSettingName, new List<CustomConfigurationSection>
                {
                    new(uniqueFirstSection, null),
                    new(uniqueSecondSection, "CustomName")
                }
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
            { uniqueSettingName, new List<CustomConfigurationSection>() }
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
            { "LogLevel", new List<CustomConfigurationSection>
                {
                    new($"Serilog{uniqueId}:MinimumLevel", "Default")
                }
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
}
