using System;
using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Abstractions.Validation;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class AttributeValidationTests
{
    #region DependsOn Attribute Tests

    [Test]
    public void CreateDataContract_WithEmptyDependsOnProperty_ShouldThrowWithPropertyName()
    {
        // Arrange
        var settings = new SettingsWithEmptyDependsOnProperty();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        Assert.That(ex!.Message, Does.Contain("DependentSetting"));
        Assert.That(ex.Message, Does.Contain("[DependsOn]"));
        Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
    }

    [Test]
    public void CreateDataContract_WithMissingValidValues_ShouldThrowWithPropertyName()
    {
        // Arrange
        var settings = new SettingsWithMissingValidValues();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        Assert.That(ex!.Message, Does.Contain("DependentSetting"));
        Assert.That(ex.Message, Does.Contain("[DependsOn]"));
        Assert.That(ex.Message, Does.Contain("At least one valid value must be specified"));
    }

    [Test]
    public void CreateDataContract_WithNonExistentDependsOnProperty_ShouldThrowWithPropertyName()
    {
        // Arrange
        var settings = new SettingsWithNonExistentProperty();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        Assert.That(ex!.Message, Does.Contain("DependentSetting"));
        Assert.That(ex.Message, Does.Contain("[DependsOn]"));
        Assert.That(ex.Message, Does.Contain("NonExistentProperty"));
        Assert.That(ex.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void CreateDataContract_WithMultipleDependsOnErrors_ShouldShowAllErrors()
    {
        // Arrange
        var settings = new SettingsWithMultipleDependsOnErrors();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        // Should contain error count
        Assert.That(ex!.Message, Does.Contain("issues found"));
        
        // Should contain both property names
        Assert.That(ex.Message, Does.Contain("DependentSetting1"));
        Assert.That(ex.Message, Does.Contain("DependentSetting2"));
    }

    [Test]
    public void CreateDataContract_WithValidDependsOn_ShouldNotThrow()
    {
        // Arrange
        var settings = new SettingsWithValidDependsOn();

        // Act & Assert
        Assert.DoesNotThrow(() => settings.CreateDataContract("TestClient"));
    }

    #endregion

    #region Heading Attribute Tests

    [Test]
    public void CreateDataContract_WithEmptyHeadingText_ShouldThrowWithPropertyName()
    {
        // Arrange
        var settings = new SettingsWithEmptyHeading();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        Assert.That(ex!.Message, Does.Contain("SettingWithBadHeading"));
        Assert.That(ex.Message, Does.Contain("[Heading]"));
        Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
    }

    #endregion

    #region Indent Attribute Tests

    [Test]
    public void CreateDataContract_WithInvalidIndentLevel_ShouldThrowWithPropertyName()
    {
        // Arrange
        var settings = new SettingsWithInvalidIndent();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        Assert.That(ex!.Message, Does.Contain("SettingWithBadIndent"));
        Assert.That(ex.Message, Does.Contain("[Indent]"));
        Assert.That(ex.Message, Does.Contain("must be between"));
    }

    #endregion

    #region Validation Attribute Tests

    [Test]
    public void CreateDataContract_WithCustomValidationMissingRegex_ShouldThrowWithPropertyName()
    {
        // Arrange
        var settings = new SettingsWithCustomValidationMissingRegex();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        Assert.That(ex!.Message, Does.Contain("SettingWithBadValidation"));
        Assert.That(ex.Message, Does.Contain("[Validation]"));
        Assert.That(ex.Message, Does.Contain("must specify a regex"));
    }

    #endregion

    #region ValidateIsBetween Attribute Tests

    [Test]
    public void CreateDataContract_WithInvalidBetweenRange_ShouldThrowWithPropertyName()
    {
        // Arrange
        var settings = new SettingsWithInvalidBetweenRange();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        Assert.That(ex!.Message, Does.Contain("NumberSetting"));
        Assert.That(ex.Message, Does.Contain("[ValidateIsBetween]"));
        Assert.That(ex.Message, Does.Contain("cannot be greater than"));
    }

    #endregion

    #region ValidateCount Attribute Tests

    [Test]
    public void CreateDataContract_WithInvalidCountRange_ShouldThrowWithPropertyName()
    {
        // Arrange
        var settings = new SettingsWithInvalidCountRange();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            settings.CreateDataContract("TestClient"));
        
        Assert.That(ex!.Message, Does.Contain("ListSetting"));
        Assert.That(ex.Message, Does.Contain("[ValidateCount]"));
        Assert.That(ex.Message, Does.Contain("cannot be greater than"));
    }

    #endregion

    #region Test Settings Classes

    // DependsOn test classes
    private class SettingsWithEmptyDependsOnProperty : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Parent setting")]
        public bool ParentSetting { get; set; } = true;

        [Setting("Dependent setting")]
        [DependsOn("", true)]
        public string DependentSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithMissingValidValues : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Parent setting")]
        public bool ParentSetting { get; set; } = true;

        [Setting("Dependent setting")]
        [DependsOn("ParentSetting")]
        public string DependentSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithNonExistentProperty : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Parent setting")]
        public bool ParentSetting { get; set; } = true;

        [Setting("Dependent setting")]
        [DependsOn("NonExistentProperty", true)]
        public string DependentSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithMultipleDependsOnErrors : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Parent setting")]
        public bool ParentSetting { get; set; } = true;

        [Setting("First dependent setting - missing valid values")]
        [DependsOn("ParentSetting")]
        public string DependentSetting1 { get; set; } = "test1";

        [Setting("Second dependent setting - missing valid values")]
        [DependsOn("ParentSetting")]
        public string DependentSetting2 { get; set; } = "test2";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithValidDependsOn : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Parent setting")]
        public bool ParentSetting { get; set; } = true;

        [Setting("Dependent setting")]
        [DependsOn("ParentSetting", true)]
        public string DependentSetting { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    // Heading test classes
    private class SettingsWithEmptyHeading : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Setting with bad heading")]
        [Heading("")]
        public string SettingWithBadHeading { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    // Indent test classes
    private class SettingsWithInvalidIndent : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Setting with bad indent")]
        [Indent(10)] // Invalid: max is 5
        public string SettingWithBadIndent { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    // Validation test classes
    private class SettingsWithCustomValidationMissingRegex : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Setting with bad validation")]
        [Validation(ValidationType.Custom)]
        public string SettingWithBadValidation { get; set; } = "test";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    // ValidateIsBetween test classes
    private class SettingsWithInvalidBetweenRange : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("Number setting")]
        [ValidateIsBetween(100, 10, Inclusion.Inclusive)] // Invalid: lower > higher
        public int NumberSetting { get; set; } = 50;

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    // ValidateCount test classes
    private class SettingsWithInvalidCountRange : SettingsBase
    {
        public override string ClientDescription => "Test settings";

        [Setting("List setting")]
        [ValidateCount(Constraint.Between, 10, 5)] // Invalid: lower > higher
        public List<ListItem> ListSetting { get; set; } = [];

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    public class ListItem
    {
        public string Name { get; set; } = "";
    }

    #endregion
}
