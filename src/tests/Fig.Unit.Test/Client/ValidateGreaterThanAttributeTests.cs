using Fig.Client.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateGreaterThanAttributeTests
{
    private ValidateGreaterThanAttribute _attribute = null!;
    private ValidateGreaterThanAttribute _attributeWithEquals = null!;

    [SetUp]
    public void Setup()
    {
        _attribute = new ValidateGreaterThanAttribute(5.0);
        _attributeWithEquals = new ValidateGreaterThanAttribute(5.0, includeEquals: true);
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

    [Test]
    public void IsValid_WithIncludeEquals_ValueGreaterThanMin_ShouldReturnTrue()
    {
        // Act
        var result = _attributeWithEquals.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithIncludeEquals_ValueEqualToMin_ShouldReturnTrue()
    {
        // Act
        var result = _attributeWithEquals.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithIncludeEquals_ValueLessThanMin_ShouldReturnFalse()
    {
        // Act
        var result = _attributeWithEquals.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("3 is not greater than or equal to 5"));
    }

    [Test]
    public void IsValid_WithIncludeEquals_NullValue_ShouldReturnFalse()
    {
        // Act
        var result = _attributeWithEquals.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not greater than or equal to 5"));
    }

    [Test]
    public void IsValid_WithIncludeEqualsAndHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateGreaterThanAttribute(5.0, includeInHealthCheck: false, includeEquals: true);

        // Act
        var result = attribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Not validated"));
    }

    [Test]
    public void GetScript_WithIncludeEquals_ShouldReturnCorrectJavaScript()
    {
        // Act
        var script = _attributeWithEquals.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring("TestProperty.Value >= 5"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be greater than or equal to 5"));
    }

    [Test]
    public void IsValid_WithIncludeEquals_IntegerTypes_ShouldWorkCorrectly()
    {
        // Test with int
        var resultInt = _attributeWithEquals.IsValid(5);
        Assert.That(resultInt.Item1, Is.True);

        // Test with long
        var resultLong = _attributeWithEquals.IsValid(5L);
        Assert.That(resultLong.Item1, Is.True);

        // Test with values less than min
        var resultIntLess = _attributeWithEquals.IsValid(4);
        Assert.That(resultIntLess.Item1, Is.False);
        Assert.That(resultIntLess.Item2, Is.EqualTo("4 is not greater than or equal to 5"));
    }
}