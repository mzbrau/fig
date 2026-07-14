using System.Collections.Generic;
using System.Linq;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class CategoryAdvancedOrderingWarningTests
{
    [Test]
    public void GetHiddenCategoryHeadingWarnings_WhenAdvancedFirstAndNonAdvancedSecondInSameCategory_ReturnsWarning()
    {
        var settings = new TestSettingsWithAdvancedFirstInCategory();
        var dataContract = settings.CreateDataContract("TestClient");

        var warnings = SettingDefinitionWarnings.GetHiddenCategoryHeadingWarnings(dataContract.Settings).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0].CategoryName, Is.EqualTo("Database"));
            Assert.That(warnings[0].FirstSettingName, Is.EqualTo("AdvancedFirstSetting"));
            Assert.That(warnings[0].NonAdvancedSettingNames, Is.EquivalentTo(new[] { "NonAdvancedSecondSetting" }));
        });
    }

    [Test]
    public void GetHiddenCategoryHeadingWarnings_WhenAllSettingsInCategoryAreAdvanced_ReturnsNoWarnings()
    {
        var settings = new TestSettingsWithAllAdvancedInCategory();
        var dataContract = settings.CreateDataContract("TestClient");

        var warnings = SettingDefinitionWarnings.GetHiddenCategoryHeadingWarnings(dataContract.Settings).ToList();

        Assert.That(warnings, Is.Empty);
    }

    [Test]
    public void GetHiddenCategoryHeadingWarnings_WhenNonAdvancedFirstInCategory_ReturnsNoWarnings()
    {
        var settings = new TestSettingsWithNonAdvancedFirstInCategory();
        var dataContract = settings.CreateDataContract("TestClient");

        var warnings = SettingDefinitionWarnings.GetHiddenCategoryHeadingWarnings(dataContract.Settings).ToList();

        Assert.That(warnings, Is.Empty);
    }

    [Test]
    public void GetHiddenCategoryHeadingWarnings_WhenCategoriesAreInterleaved_ReturnsWarningForAffectedCategory()
    {
        var settings = new TestSettingsWithInterleavedAdvancedCategory();
        var dataContract = settings.CreateDataContract("TestClient");

        var warnings = SettingDefinitionWarnings.GetHiddenCategoryHeadingWarnings(dataContract.Settings).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0].CategoryName, Is.EqualTo("Database"));
            Assert.That(warnings[0].FirstSettingName, Is.EqualTo("Database1"));
            Assert.That(warnings[0].NonAdvancedSettingNames, Is.EquivalentTo(new[] { "Database2" }));
        });
    }

    [Test]
    public void GetHiddenCategoryHeadingWarnings_WhenNoCategoryOnSettings_ReturnsNoWarnings()
    {
        var settings = new TestSettingsWithoutCategoryForWarnings();
        var dataContract = settings.CreateDataContract("TestClient");

        var warnings = SettingDefinitionWarnings.GetHiddenCategoryHeadingWarnings(dataContract.Settings).ToList();

        Assert.That(warnings, Is.Empty);
    }

    [Test]
    public void GetHiddenCategoryHeadingWarnings_WhenMultipleNonAdvancedFollowers_ListsAllFollowers()
    {
        var settings = new TestSettingsWithMultipleNonAdvancedFollowers();
        var dataContract = settings.CreateDataContract("TestClient");

        var warnings = SettingDefinitionWarnings.GetHiddenCategoryHeadingWarnings(dataContract.Settings).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0].NonAdvancedSettingNames, Is.EquivalentTo(new[] { "SecondSetting", "ThirdSetting" }));
        });
    }

    private class TestSettingsWithAdvancedFirstInCategory : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Advanced first setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        [Advanced]
        public string AdvancedFirstSetting { get; set; } = "default";

        [Setting("Non-advanced second setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string NonAdvancedSecondSetting { get; set; } = "default";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class TestSettingsWithAllAdvancedInCategory : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Advanced first setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        [Advanced]
        public string AdvancedFirstSetting { get; set; } = "default";

        [Setting("Advanced second setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        [Advanced]
        public string AdvancedSecondSetting { get; set; } = "default";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class TestSettingsWithNonAdvancedFirstInCategory : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Non-advanced first setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string NonAdvancedFirstSetting { get; set; } = "default";

        [Setting("Advanced second setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        [Advanced]
        public string AdvancedSecondSetting { get; set; } = "default";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class TestSettingsWithInterleavedAdvancedCategory : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("First database setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        [Advanced]
        public string Database1 { get; set; } = "default";

        [Setting("Logging setting")]
        [Fig.Client.Abstractions.Attributes.Category("Logging", "#FF6600")]
        public string Logging1 { get; set; } = "default";

        [Setting("Second database setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string Database2 { get; set; } = "default";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class TestSettingsWithoutCategoryForWarnings : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Setting without category")]
        public string SettingWithoutCategory { get; set; } = "default";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class TestSettingsWithMultipleNonAdvancedFollowers : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Advanced first setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        [Advanced]
        public string FirstSetting { get; set; } = "default";

        [Setting("Non-advanced second setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string SecondSetting { get; set; } = "default";

        [Setting("Non-advanced third setting")]
        [Fig.Client.Abstractions.Attributes.Category("Database", "#0066CC")]
        public string ThirdSetting { get; set; } = "default";

        public override IEnumerable<string> GetValidationErrors() => [];
    }
}
