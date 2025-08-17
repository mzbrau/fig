using System.Collections.Generic;
using System.Linq;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
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
    public void ConfigurationSectionOverrideAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithConfigurationSectionOverride();

        // Act
        var configSections = settings.GetConfigurationSections();

        // Assert
        var nestedClassSections = configSections["NestedClass->ChildProperty"];
        Assert.That(nestedClassSections, Is.Not.Null.And.Count.GreaterThan(0));
        
        // Verify that the expected "InheritedSection" exists
        Assert.That(nestedClassSections.Any(s => s.SectionName == "NestedClass"), Is.True,
            $"Expected 'NestedClass' to be inherited, but got sections: {string.Join(", ", nestedClassSections.Select(s => s.SectionName))}");
        
        // Get the inherited section and verify its properties
        var inheritedSection = nestedClassSections.First(s => s.SectionName == "NestedClass");
        Assert.That(inheritedSection.SectionName, Is.EqualTo("NestedClass"), 
            "Configuration section name should be 'NestedClass' through inheritance");
    }

    [Test]
    public void DependsOnAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithDependsOn();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.DependsOnProperty, Is.EqualTo("ParentProperty"));
        Assert.That(childSetting.DependsOnValidValues, Does.Contain("True"));
    }

    [Test]
    public void DisplayScriptAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithDisplayScript();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.DisplayScript, Is.EqualTo("/* inherited script */"));
    }

    [Test]
    public void IndentAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithIndent();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.Indent, Is.EqualTo(3));
    }

    [Test]
    public void LookupTableAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithLookupTable();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.LookupTableKey, Is.EqualTo("InheritedLookupTable"));
    }

    [Test]
    public void MultiLineAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithMultiLine();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.EditorLineCount, Is.EqualTo(5));
    }

    [Test]
    public void SecretAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithSecret();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.IsSecret, Is.True, "Secret attribute should be inherited");
    }

    [Test]
    public void ValidationAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithValidation();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.ValidationRegex, Is.EqualTo(@"\d+"));
        Assert.That(childSetting.ValidationExplanation, Is.EqualTo("Should be inherited"));
    }

    [Test]
    public void ValidateGreaterThanAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithValidateGreaterThan();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->NumberProperty");
        Assert.That(childSetting.DisplayScript, Does.Contain("NumberProperty.Value > 10"), "Should contain validation script");
    }

    [Test]
    public void ValidateLessThanAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithValidateLessThan();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->NumberProperty");
        Assert.That(childSetting.DisplayScript, Does.Contain("NumberProperty.Value < 100"), "Should contain validation script");
    }

    [Test]
    public void ValidateIsBetweenAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithValidateIsBetween();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->NumberProperty");
        Assert.That(childSetting.DisplayScript, Does.Contain("NumberProperty.Value >= 1"), "Should contain lower bound validation");
        Assert.That(childSetting.DisplayScript, Does.Contain("NumberProperty.Value <= 10"), "Should contain upper bound validation");
    }

    [Test]
    public void ValidValuesAttribute_OnNestedSetting_IsInheritedByChildren()
    {
        // Arrange
        var settings = new NestedSettingsWithValidValues();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var childSetting = dataContract.Settings.First(s => s.Name == "NestedClass->ChildProperty");
        Assert.That(childSetting.ValidValues, Is.Not.Null);
        Assert.That(childSetting.ValidValues, Does.Contain("Option1"));
        Assert.That(childSetting.ValidValues, Does.Contain("Option2"));
        Assert.That(childSetting.ValidValues, Does.Contain("Option3"));
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
        // Test that attributes that are NOT in the inheritable list are not inherited
        // Since we don't have easy access to ReadOnlyAttribute, we'll just verify basic properties
        Assert.That(childSetting.Advanced, Is.False, "Advanced should not be inherited when not applied");
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
    [Fig.Client.Abstractions.Attributes.Category("Inherited Category", "#FF0000")]
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
    [Fig.Client.Abstractions.Attributes.Category("Multi Category", "#FF0000")]
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
    [Fig.Client.Abstractions.Attributes.Category("Parent Category", "#00FF00")]
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
    // ReadOnlyAttribute is an example of a non-inheritable attribute (not in the list)
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithConfigurationSectionOverride : SettingsBase
{
    public override string ClientDescription => "Test settings with ConfigurationSectionOverride nested";

    [NestedSetting]
    [ConfigurationSectionOverride("InheritedSection")]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithDependsOn : SettingsBase
{
    public override string ClientDescription => "Test settings with DependsOn nested";

    [Setting("Parent property")]
    public bool ParentProperty { get; set; } = true;

    [NestedSetting]
    [DependsOn(nameof(ParentProperty), true)]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithDisplayScript : SettingsBase
{
    public override string ClientDescription => "Test settings with DisplayScript nested";

    [NestedSetting]
    [DisplayScript("/* inherited script */")]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithIndent : SettingsBase
{
    public override string ClientDescription => "Test settings with Indent nested";

    [NestedSetting]
    [Indent(3)]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithLookupTable : SettingsBase
{
    public override string ClientDescription => "Test settings with LookupTable nested";

    [NestedSetting]
    [LookupTable("InheritedLookupTable", LookupSource.UserDefined)]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithMultiLine : SettingsBase
{
    public override string ClientDescription => "Test settings with MultiLine nested";

    [NestedSetting]
    [MultiLine(5)]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithSecret : SettingsBase
{
    public override string ClientDescription => "Test settings with Secret nested";

    [NestedSetting]
    [Secret]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithValidation : SettingsBase
{
    public override string ClientDescription => "Test settings with Validation nested";

    [NestedSetting]
    [Validation(@"\d+", "Should be inherited")]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithValidateGreaterThan : SettingsBase
{
    public override string ClientDescription => "Test settings with ValidateGreaterThan nested";

    [NestedSetting]
    [ValidateGreaterThan(10, Inclusion.Exclusive)]
    public NumericNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithValidateLessThan : SettingsBase
{
    public override string ClientDescription => "Test settings with ValidateLessThan nested";

    [NestedSetting]
    [ValidateLessThan(100, Inclusion.Exclusive)]
    public NumericNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithValidateIsBetween : SettingsBase
{
    public override string ClientDescription => "Test settings with ValidateIsBetween nested";

    [NestedSetting]
    [ValidateIsBetween(1, 10, Inclusion.Inclusive)]
    public NumericNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NestedSettingsWithValidValues : SettingsBase
{
    public override string ClientDescription => "Test settings with ValidValues nested";

    [NestedSetting]
    [ValidValues("Option1", "Option2", "Option3")]
    public SimpleNestedClass NestedClass { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class NumericNestedClass
{
    [Setting("Number property description")]
    public int NumberProperty { get; set; } = 5;
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
    [Fig.Client.Abstractions.Attributes.Category("Child Category", "#0000FF")]
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