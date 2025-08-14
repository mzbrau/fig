using Fig.Api.Enums;
using Fig.Client.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateGreaterThanAttributeTests
{
    private ValidateGreaterThanAttribute _exclusiveAttribute = null!;
    private ValidateGreaterThanAttribute _inclusiveAttribute = null!;

    [SetUp]
    public void Setup()
    {
        _exclusiveAttribute = new ValidateGreaterThanAttribute(5.0, Inclusion.Exclusive);
        _inclusiveAttribute = new ValidateGreaterThanAttribute(5.0, Inclusion.Inclusive);
    }

    [Test]
    public void ApplyToTypes_ShouldReturnCorrectTypes()
    {
        // Act
        var types = _exclusiveAttribute.ApplyToTypes;

        // Assert
        Assert.That(types, Contains.Item(typeof(double)));
        Assert.That(types, Contains.Item(typeof(int)));
        Assert.That(types, Contains.Item(typeof(long)));
    }

    [Test]
    public void IsValid_Exclusive_WithValueGreaterThanMin_ShouldReturnTrue()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Exclusive_WithValueEqualToMin_ShouldReturnFalse()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("5 is not greater than 5"));
    }

    [Test]
    public void IsValid_Exclusive_WithValueLessThanMin_ShouldReturnFalse()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("3 is not greater than 5"));
    }

    [Test]
    public void IsValid_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not greater than 5"));
    }

    [Test]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateGreaterThanAttribute(5.0, Inclusion.Exclusive, includeInHealthCheck: false);

        // Act
        var result = attribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Not validated"));
    }

    [Test]
    public void GetScript_Exclusive_ShouldReturnCorrectJavaScript()
    {
        // Act
        var script = _exclusiveAttribute.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring("TestProperty.Value > 5"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be greater than 5"));
    }

    [Test]
    public void IsValid_Inclusive_WithValueGreaterThanMin_ShouldReturnTrue()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Inclusive_WithValueEqualToMin_ShouldReturnTrue()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Inclusive_WithValueLessThanMin_ShouldReturnFalse()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("3 is not greater than or equal to 5"));
    }

    [Test]
    public void IsValid_Inclusive_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not greater than or equal to 5"));
    }

    [Test]
    public void IsValid_Inclusive_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateGreaterThanAttribute(5.0, Inclusion.Inclusive, includeInHealthCheck: false);

        // Act
        var result = attribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Not validated"));
    }

    [Test]
    public void GetScript_Inclusive_ShouldReturnCorrectJavaScript()
    {
        // Act
        var script = _inclusiveAttribute.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring("TestProperty.Value >= 5"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be greater than or equal to 5"));
    }

    [Test]
    public void IsValid_Inclusive_IntegerTypes_ShouldWorkCorrectly()
    {
        // Test with int
        var resultInt = _inclusiveAttribute.IsValid(5);
        Assert.That(resultInt.Item1, Is.True);

        // Test with long
        var resultLong = _inclusiveAttribute.IsValid(5L);
        Assert.That(resultLong.Item1, Is.True);

        // Test with values less than min
        var resultIntLess = _inclusiveAttribute.IsValid(4);
        Assert.That(resultIntLess.Item1, Is.False);
        Assert.That(resultIntLess.Item2, Is.EqualTo("4 is not greater than or equal to 5"));
    }
}