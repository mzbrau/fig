using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

[TestFixture]
public class SettingsBaseTests
{
    [Test]
    public void ShallSetDefaultValues()
    {
        var settings = new TestSettings();
        settings.Initialize(null);

        Assert.That(settings.StringSetting, Is.EqualTo("test"));
        Assert.That(settings.IntSetting, Is.EqualTo(4));
    }

    [Test]
    public void ShallSetValuesFromDataContract()
    {
        const string stringValue = "From data contract";
        const int intValue = 10;

        var settingsDataContracts = new List<SettingDataContract>
        {
            new("StringSetting", stringValue),
            new("IntSetting", intValue)
        };

        var settings = new TestSettings();
        settings.Initialize(settingsDataContracts);

        Assert.That(settings.StringSetting, Is.EqualTo(stringValue));
        Assert.That(settings.IntSetting, Is.EqualTo(intValue));
    }

    [Test]
    public void ShallConvertTopLevelProperties()
    {
        var settings = new TestSettings();
        var dataContract = settings.CreateDataContract();

        Assert.That(dataContract.Name, Is.EqualTo(settings.ClientName));
        Assert.That(dataContract.Settings.Count, Is.EqualTo(4));
    }

    [Test]
    public void ShallConvertStringSetting()
    {
        AssertSettingIsMatch(CreateDataContract(), "StringSetting", "String Setting",
            "This is a test setting", true, "test", ValidationType.Custom, @"(.*[a-z]){3,}",
            "Must have at least 3 characters", null, "My Group", 1);
    }

    [Test]
    public void ShallConvertIntSetting()
    {
        AssertSettingIsMatch(CreateDataContract(), "IntSetting", "Int Setting",
            "This is an int setting", false, 4, ValidationType.None, null,
            null, null, null, 2);
    }

    [Test]
    public void ShallConvertEnumSetting()
    {
        AssertSettingIsMatch(CreateDataContract(), "EnumSetting", "Enum Setting",
            "An Enum Setting", false, TestEnum.Item2.ToString(), ValidationType.None, null,
            null, Enum.GetNames<TestEnum>().ToList(), null, null);
    }

    [Test]
    public void ShallConvertListSetting()
    {
        AssertSettingIsMatch(CreateDataContract(), "ListSetting", "List Setting",
                "A List", false, null, ValidationType.None, null,
            null, null, null, null);
    }

    private SettingsClientDefinitionDataContract CreateDataContract()
    {
        var settings = new TestSettings();
        return settings.CreateDataContract();
    }

    private void AssertSettingIsMatch(
            SettingsClientDefinitionDataContract dataContract,
            string name,
            string friendlyName,
            string description,
            bool isSecret,
            object? defaultValue,
            ValidationType validationType,
            string? validationRegex,
            string? validationExplanation,
            List<string>? validValues,
            string? group,
            int? displayOrder)
    {
        var setting = dataContract.Settings.FirstOrDefault(a => a.Name == name);

        if (setting == null)
        {
            Assert.Fail($"Setting with name {name} not found.");
            return;
        }

        Assert.That(setting.Description, Is.EqualTo(description));
        Assert.That(setting.IsSecret, Is.EqualTo(isSecret));
        Assert.That(setting.DefaultValue, Is.EqualTo(defaultValue));
        Assert.That(setting.ValidationType, Is.EqualTo(validationType));
        Assert.That(setting.ValidationRegex, Is.EqualTo(validationRegex));
        Assert.That(setting.ValidationExplanation, Is.EqualTo(validationExplanation));
        Assert.That(setting.Group, Is.EqualTo(group));
        Assert.That(setting.DisplayOrder, Is.EqualTo(displayOrder));

        if (setting.ValidValues != null || validValues != null)
        {
            Assert.That(setting.ValidValues, Is.EquivalentTo(validValues));
        }
    }
}