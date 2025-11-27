using System.Collections.Generic;
using Fig.Client.ExternallyManaged;
using NUnit.Framework;

namespace Fig.Unit.Test.Client.ExternallyManaged;

[TestFixture]
public class FigValuesStoreTests
{
    [SetUp]
    public void Setup()
    {
        FigValuesStore.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        FigValuesStore.Clear();
    }

    [Test]
    public void StoreFigValues_ShouldStoreValues()
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2"
        };

        // Act
        FigValuesStore.StoreFigValues(values);
        var result = FigValuesStore.GetFigValues();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result["Key1"], Is.EqualTo("Value1"));
        Assert.That(result["Key2"], Is.EqualTo("Value2"));
    }

    [Test]
    public void GetFigValues_ShouldReturnCopy()
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["Key1"] = "Value1"
        };
        FigValuesStore.StoreFigValues(values);

        // Act
        var result1 = FigValuesStore.GetFigValues();
        result1["NewKey"] = "NewValue";
        var result2 = FigValuesStore.GetFigValues();

        // Assert
        Assert.That(result2.ContainsKey("NewKey"), Is.False, "Modifying returned dictionary should not affect stored values");
    }

    [Test]
    public void Clear_ShouldRemoveAllValues()
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["Key1"] = "Value1"
        };
        FigValuesStore.StoreFigValues(values);

        // Act
        FigValuesStore.Clear();
        var result = FigValuesStore.GetFigValues();

        // Assert
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void StoreFigValues_CalledMultipleTimes_ShouldMergeValues()
    {
        // Arrange & Act
        FigValuesStore.StoreFigValues(new Dictionary<string, string?> { ["Key1"] = "Value1" });
        FigValuesStore.StoreFigValues(new Dictionary<string, string?> { ["Key2"] = "Value2" });
        
        var result = FigValuesStore.GetFigValues();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result["Key1"], Is.EqualTo("Value1"));
        Assert.That(result["Key2"], Is.EqualTo("Value2"));
    }

    [Test]
    public void StoreFigValues_WithSameKey_ShouldOverwriteValue()
    {
        // Arrange & Act
        FigValuesStore.StoreFigValues(new Dictionary<string, string?> { ["Key1"] = "OldValue" });
        FigValuesStore.StoreFigValues(new Dictionary<string, string?> { ["Key1"] = "NewValue" });
        
        var result = FigValuesStore.GetFigValues();

        // Assert
        Assert.That(result["Key1"], Is.EqualTo("NewValue"));
    }
}
