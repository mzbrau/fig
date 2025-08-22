using System.Linq;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class CustomCategoryAttributeTests
{
    [Test]
    public void CategoryHelper_GetName_ExtractsCorrectName()
    {
        // Arrange
        var customCategory = TestCustomCategory.CustomCategory1;

        // Act
        var name = CategoryHelper.GetName(customCategory);

        // Assert
        Assert.That(name, Is.EqualTo("My Custom Category"));
    }

    [Test]
    public void CategoryHelper_GetColorHex_ExtractsCorrectColor()
    {
        // Arrange
        var customCategory = TestCustomCategory.CustomCategory1;

        // Act
        var colorHex = CategoryHelper.GetColorHex(customCategory);

        // Assert
        Assert.That(colorHex, Is.EqualTo("#FF5733"));
    }

    [Test]
    public void CategoryHelper_GetName_WithoutAttribute_ReturnsNull()
    {
        // Arrange
        var customCategory = TestCustomCategory.CategoryWithoutAttributes;

        // Act
        var name = CategoryHelper.GetName(customCategory);

        // Assert
        Assert.That(name, Is.Null);
    }

    [Test]
    public void CategoryHelper_GetColorHex_WithoutAttribute_ReturnsNull()
    {
        // Arrange
        var customCategory = TestCustomCategory.CategoryWithoutAttributes;

        // Act
        var colorHex = CategoryHelper.GetColorHex(customCategory);

        // Assert
        Assert.That(colorHex, Is.Null);
    }

    [Test]
    public void CategoryHelper_FromEnum_CreatesCorrectCategoryAttribute()
    {
        // Arrange
        var customCategory = TestCustomCategory.CustomCategory1;

        // Act
        var categoryAttribute = CategoryHelper.FromEnum(customCategory);

        // Assert
        Assert.That(categoryAttribute.Name, Is.EqualTo("My Custom Category"));
        Assert.That(categoryAttribute.ColorHex, Is.EqualTo("#FF5733"));
    }

    [Test]
    public void CategoryHelper_FromEnum_WithoutAttributes_UsesDefaults()
    {
        // Arrange
        var customCategory = TestCustomCategory.CategoryWithoutAttributes;

        // Act
        var categoryAttribute = CategoryHelper.FromEnum(customCategory);

        // Assert
        Assert.That(categoryAttribute.Name, Is.EqualTo("CategoryWithoutAttributes"));
        Assert.That(categoryAttribute.ColorHex, Is.EqualTo("#000000"));
    }

    [Test]
    public void CategoryAttribute_WithPredefinedEnum_StillWorks()
    {
        // Arrange
        var predefinedCategory = Category.Database;

        // Act
        var categoryAttribute = new Fig.Client.Attributes.CategoryAttribute(predefinedCategory);

        // Assert
        Assert.That(categoryAttribute.Name, Is.EqualTo("Database"));
        Assert.That(categoryAttribute.ColorHex, Is.EqualTo("#4f51c9"));
    }

    [Test]
    public void CategoryAttribute_WithCustomNameAndColor_Works()
    {
        // Arrange & Act
        var categoryAttribute = new Fig.Client.Attributes.CategoryAttribute("My Custom Category", "#FF5733");

        // Assert
        Assert.That(categoryAttribute.Name, Is.EqualTo("My Custom Category"));
        Assert.That(categoryAttribute.ColorHex, Is.EqualTo("#FF5733"));
    }

    [Test]
    public void CategoryAttribute_WithCustomEnumInSettingsClass_ExtractsCorrectly()
    {
        // Arrange
        var settings = new TestSettingsWithCustomCategory();

        // Act
        var dataContract = settings.CreateDataContract("TestClient");

        // Assert
        var setting = dataContract.Settings.First(s => s.Name == nameof(TestSettingsWithCustomCategory.TestSetting));
        Assert.That(setting.CategoryName, Is.EqualTo("My Custom Category"));
        Assert.That(setting.CategoryColor, Is.EqualTo("#FF5733"));
    }
}

public enum TestCustomCategory
{
    [CategoryName("My Custom Category")]
    [ColorHex("#FF5733")]
    CustomCategory1,
    
    [CategoryName("Another Custom Category")]
    [ColorHex("#33FF57")]
    CustomCategory2,
    
    CategoryWithoutAttributes
}

public class TestSettingsWithCustomCategory : Fig.Client.SettingsBase
{
    public override string ClientDescription => "Test settings with custom category";

    [Fig.Client.Attributes.Category("My Custom Category", "#FF5733")]
    [Fig.Client.Attributes.Setting("Test Setting")]
    public string TestSetting { get; set; } = "Default Value";

    public override System.Collections.Generic.IEnumerable<string> GetValidationErrors() => [];
}