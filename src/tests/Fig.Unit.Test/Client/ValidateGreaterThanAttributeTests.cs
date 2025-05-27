using Fig.Client.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateGreaterThanAttributeTests
{
    private ValidateGreaterThanAttribute _attribute;

    [SetUp]
    public void Setup()
    {
        _attribute = new ValidateGreaterThanAttribute(5.0);
    }

    [Test]
    public void ApplyToTypes_ShouldReturnCorrectTypes()
    {
        // Act
        var types = _attribute.ApplyToTypes;

        // Assert
        Assert.That(types, Contains.Item(typeof(double)));
        Assert.That(types, Contains.Item(typeof(int)));
        Assert.That(types, Contains.Item(typeof(long)));
    }

    [Test]
    public void IsValid_WithValueGreaterThanMin_ShouldReturnTrue()
    {
        // Act
        var result = _attribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithValueEqualToMin_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("5 is not greater than 5"));
    }

    [Test]
    public void IsValid_WithValueLessThanMin_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("3 is not greater than 5"));
    }

    [Test]
    public void IsValid_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not greater than 5"));
    }

    [Test]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateGreaterThanAttribute(5.0, includeInHealthCheck: false);

        // Act
        var result = attribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Not validated"));
    }

    [Test]
    public void GetScript_ShouldReturnCorrectJavaScript()
    {
        // Act
        var script = _attribute.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring("TestProperty.Value > 5"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be greater than 5"));
    }
}