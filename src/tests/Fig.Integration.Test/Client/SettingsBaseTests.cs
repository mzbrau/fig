using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Contracts.SettingDefinitions;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

[TestFixture]
public class SettingsBaseTests
{
    [Test]
    public void ShallConvertTopLevelProperties()
    {
        var settings = new TestSettings();
        var dataContract = settings.CreateDataContract(settings.ClientName);

        Assert.That(dataContract.Name, Is.EqualTo(settings.ClientName));
        Assert.That(dataContract.Settings.Count, Is.EqualTo(4));
    }

    [Test]
    public void ShallConvertStringSetting()
    {
        AssertSettingIsMatch(CreateDataContract(), "StringSetting",
            "This is a test setting", true, "test", @"(.*[a-z]){3,}",
            "Must have at least 3 characters", null, "My Group", 1, true, null, null);
    }

    [Test]
    public void ShallConvertIntSetting()
    {
        AssertSettingIsMatch(CreateDataContract(), "IntSetting",
            "This is an int setting", false, 4, null,
            null, null, null, 2, true, "#cc4e58", "Test");
    }

    [Test]
    public void ShallConvertEnumSetting()
    {
        AssertSettingIsMatch(CreateDataContract(), "EnumSetting",
            "An Enum Setting", false, TestEnum.Item2.ToString(), null,
            null, Enum.GetNames<TestEnum>().ToList(), null, null, true, null, null);
    }

    [Test]
    public void ShallConvertListSetting()
    {
        AssertSettingIsMatch(CreateDataContract(), "ListSetting",
                "A List", false, null, null,
            null, null, null, null, true, null, null);
    }

    private SettingsClientDefinitionDataContract CreateDataContract()
    {
        var settings = new TestSettings();
        return settings.CreateDataContract(settings.ClientName);
    }

    private void AssertSettingIsMatch(
            SettingsClientDefinitionDataContract dataContract,
            string name,
            string description,
            bool isSecret,
            object? defaultValue,
            string? validationRegex,
            string? validationExplanation,
            List<string>? validValues,
            string? group,
            int? displayOrder,
            bool supportsLiveUpdate,
            string? categoryColor,
            string? categoryName)
    {
        var setting = dataContract.Settings.FirstOrDefault(a => a.Name == name);

        if (setting == null)
        {
            Assert.Fail($"Setting with name {name} not found.");
            return;
        }

        Assert.That(setting.Description, Is.EqualTo(description));
        Assert.That(setting.IsSecret, Is.EqualTo(isSecret));
        Assert.That(setting.DefaultValue?.GetValue(), Is.EqualTo(defaultValue));
        Assert.That(setting.ValidationRegex, Is.EqualTo(validationRegex));
        Assert.That(setting.ValidationExplanation, Is.EqualTo(validationExplanation));
        Assert.That(setting.Group, Is.EqualTo(group));
        Assert.That(setting.DisplayOrder, Is.EqualTo(displayOrder));
        Assert.That(setting.SupportsLiveUpdate, Is.EqualTo(supportsLiveUpdate));
        Assert.That(setting.CategoryColor, Is.EqualTo(categoryColor));
        Assert.That(setting.CategoryName, Is.EqualTo(categoryName));

        if (setting.ValidValues != null || validValues != null)
        {
            Assert.That(setting.ValidValues, Is.EquivalentTo(validValues));
        }
    }
}