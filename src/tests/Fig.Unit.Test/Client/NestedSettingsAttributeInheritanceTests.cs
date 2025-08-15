using System.Collections.Generic;
using System.Linq;
using Fig.Client;
using Fig.Client.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class NestedSettingsAttributeInheritanceTests
{
    [Test]
    public void AdvancedAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithAdvanced();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.Advanced, Is.True, "Child property should inherit Advanced attribute from nested setting");
    }

    [Test]
    public void CategoryAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithCategory();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.CategoryName, Is.EqualTo("Inherited Category"));
        Assert.That(childSetting.CategoryColor, Is.EqualTo("#FF0000"));
    }

    [Test]
    public void GroupAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithGroup();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.Group, Is.EqualTo("Inherited Group"));
    }

    [Test]
    public void EnvironmentSpecificAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithEnvironmentSpecific();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.EnvironmentSpecific, Is.True);
    }

    [Test]
    public void MultipleInheritableAttributes_OnNestedSetting_AreAllInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithMultipleAttributes();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.Advanced, Is.True);
        Assert.That(childSetting.CategoryName, Is.EqualTo("Multi Category"));
        Assert.That(childSetting.Group, Is.EqualTo("Multi Group"));
        Assert.That(childSetting.EnvironmentSpecific, Is.True);
    }

    [Test]
    public void DirectAttributeOnChild_OverridesInheritedAttribute()
    {
        // Arrange
        var settings = new NestedSettingsWithOverride();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildWithOwnCategory");
        Assert.That(childSetting.CategoryName, Is.EqualTo("Child Category"), "Direct attribute should override inherited");
        Assert.That(childSetting.Advanced, Is.True, "Other inherited attributes should still apply");
    }

    [Test]
    public void DeepNesting_InheritsAttributesFromAllLevels()
    {
        // Arrange
        var settings = new DeepNestedInheritanceSettings();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var deepChildSetting = dataContract.Settings.First(s => s.Name == "Level1->Level2->DeepProperty");
        Assert.That(deepChildSetting.Advanced, Is.True, "Should inherit Advanced from Level1");
        Assert.That(deepChildSetting.Group, Is.EqualTo("Level2 Group"), "Should inherit Group from Level2");
    }

    [Test]
    public void NonInheritableAttributes_AreNotInherited()
    {
        // Arrange
        var settings = new NestedSettingsWithNonInheritableAttributes();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.IsSecret, Is.False, "Secret attribute should not be inherited");
        Assert.That(childSetting.ValidationRegex, Is.Null, "Validation attribute should not be inherited");
    }

    [Test]
    public void SettingWithoutNestedInheritance_DoesNotHaveAdvancedAttribute()
    {
        // Arrange - this test verifies that without inheritance, child properties don't get the attribute
        var settings = new NestedSettingsWithoutInheritance();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.Advanced, Is.False, "Child property should NOT have Advanced attribute when parent doesn't have it");
    }
}

// Test classes

public class NestedSettingsWithAdvanced : SettingsBase
{
    public override string ClientDescription => "Test settings with Advanced nested";

    [NestedSetting]
    [Advanced]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithCategory : SettingsBase
{
    public override string ClientDescription => "Test settings with Category nested";

    [NestedSetting]
    [Fig.Client.Attributes.Category("Inherited Category", "#FF0000")]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithGroup : SettingsBase
{
    public override string ClientDescription => "Test settings with Group nested";

    [NestedSetting]
    [Group("Inherited Group")]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithEnvironmentSpecific : SettingsBase
{
    public override string ClientDescription => "Test settings with EnvironmentSpecific nested";

    [NestedSetting]
    [EnvironmentSpecific]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithMultipleAttributes : SettingsBase
{
    public override string ClientDescription => "Test settings with multiple attributes nested";

    [NestedSetting]
    [Advanced]
    [Fig.Client.Attributes.Category("Multi Category", "#FF0000")]
    [Group("Multi Group")]
    [EnvironmentSpecific]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithOverride : SettingsBase
{
    public override string ClientDescription => "Test settings with override";

    [NestedSetting]
    [Advanced]
    [Fig.Client.Attributes.Category("Parent Category", "#00FF00")]
    public NestedClassWithOverride NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class DeepNestedInheritanceSettings : SettingsBase
{
    public override string ClientDescription => "Test settings with deep nesting";

    [NestedSetting]
    [Advanced]
    public Level1Class Level1 { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithNonInheritableAttributes : SettingsBase
{
    public override string ClientDescription => "Test settings with non-inheritable attributes";

    [NestedSetting]
    [Secret]
    [Validation(@"\d+", "Should not be inherited")]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithoutInheritance : SettingsBase
{
    public override string ClientDescription => "Test settings without inheritance";

    [NestedSetting]
    // No [Advanced] attribute here
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class SimpleNestedClass
{
    [Setting("Child property description")]
    public string ChildProperty { get; set; } = "default";
}

public class NestedClassWithOverride
{
    [Setting("Child property description")]
    [Fig.Client.Attributes.Category("Child Category", "#0000FF")]
    public string ChildWithOwnCategory { get; set; } = "default";
}

public class Level1Class
{
    [NestedSetting]
    [Group("Level2 Group")]
    public Level2Class Level2 { get; set; } = new();
}

public class Level2Class
{
    [Setting("Deep property description")]
    public string DeepProperty { get; set; } = "default";
}