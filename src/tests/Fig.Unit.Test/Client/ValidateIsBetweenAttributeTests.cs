using Fig.Api.Enums;
using Fig.Client.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateIsBetweenAttributeTests
{
    private ValidateIsBetweenAttribute _inclusiveAttribute = null!;
    private ValidateIsBetweenAttribute _exclusiveAttribute = null!;

    [SetUp]
    public void Setup()
    {
        _inclusiveAttribute = new ValidateIsBetweenAttribute(5.0, 10.0, Inclusion.Inclusive);
        _exclusiveAttribute = new ValidateIsBetweenAttribute(5.0, 10.0, Inclusion.Exclusive);
    }

    [Test]
    public void ApplyToTypes_ShouldReturnCorrectTypes()
    {
        // Act
        var types = _inclusiveAttribute.ApplyToTypes;

        // Assert
        Assert.That(types, Contains.Item(typeof(double)));
        Assert.That(types, Contains.Item(typeof(int)));
        Assert.That(types, Contains.Item(typeof(long)));
    }

    [Test]
    public void IsValid_Inclusive_WithValueInRange_ShouldReturnTrue()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(7.5);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Inclusive_WithValueAtLowerBound_ShouldReturnTrue()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Inclusive_WithValueAtUpperBound_ShouldReturnTrue()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Exclusive_WithValueInRange_ShouldReturnTrue()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(7.5);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Exclusive_WithValueAtLowerBound_ShouldReturnFalse()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("5 is not between (exclusive) 5 and 10"));
    }

    [Test]
    public void IsValid_Exclusive_WithValueAtUpperBound_ShouldReturnFalse()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("10 is not between (exclusive) 5 and 10"));
    }

    [Test]
    public void IsValid_WithValueBelowRange_ShouldReturnFalse()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("3 is not between (inclusive) 5 and 10"));
    }

    [Test]
    public void IsValid_WithValueAboveRange_ShouldReturnFalse()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(15.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("15 is not between (inclusive) 5 and 10"));
    }

    [Test]
    public void IsValid_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not between (inclusive) 5 and 10"));
    }

    [Test]
    public void IsValid_WithIntegerValue_ShouldWork()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(8);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithLongValue_ShouldWork()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(9L);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithNonNumericType_ShouldReturnFalse()
    {
        // Act
        var result = _inclusiveAttribute.IsValid("not a number");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("not a number is not between (inclusive) 5 and 10"));
    }

    [Test]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateIsBetweenAttribute(5.0, 10.0, Inclusion.Inclusive, includeInHealthCheck: false);

        // Act
        var result = attribute.IsValid(15.0);

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
        Assert.That(script, Contains.Substring("TestProperty.Value <= 10"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be between (inclusive) 5 and 10"));
    }

    [Test]
    public void GetScript_Exclusive_ShouldReturnCorrectJavaScript()
    {
        // Act
        var script = _exclusiveAttribute.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring("TestProperty.Value > 5"));
        Assert.That(script, Contains.Substring("TestProperty.Value < 10"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be between (exclusive) 5 and 10"));
    }
}