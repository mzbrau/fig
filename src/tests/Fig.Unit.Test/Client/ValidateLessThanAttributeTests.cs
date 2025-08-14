using Fig.Api.Enums;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateLessThanAttributeTests
{
    private ValidateLessThanAttribute _exclusiveAttribute = null!;
    private ValidateLessThanAttribute _inclusiveAttribute = null!;

    [SetUp]
    public void Setup()
    {
        _exclusiveAttribute = new ValidateLessThanAttribute(10.0, Inclusion.Exclusive);
        _inclusiveAttribute = new ValidateLessThanAttribute(10.0, Inclusion.Inclusive);
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
    public void IsValid_Exclusive_WithValueLessThanMax_ShouldReturnTrue()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Exclusive_WithValueEqualToMax_ShouldReturnFalse()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("10 is not less than 10"));
    }

    [Test]
    public void IsValid_Exclusive_WithValueGreaterThanMax_ShouldReturnFalse()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(15.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("15 is not less than 10"));
    }

    [Test]
    public void IsValid_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _exclusiveAttribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not less than 10"));
    }

    [Test]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateLessThanAttribute(10.0, Inclusion.Exclusive, includeInHealthCheck: false);

        // Act
        var result = attribute.IsValid(15.0);

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
        Assert.That(script, Contains.Substring("TestProperty.Value < 10"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be less than 10"));
    }

    [Test]
    public void IsValid_Inclusive_WithValueLessThanMax_ShouldReturnTrue()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Inclusive_WithValueEqualToMax_ShouldReturnTrue()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_Inclusive_WithValueGreaterThanMax_ShouldReturnFalse()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(15.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("15 is not less than or equal to 10"));
    }

    [Test]
    public void IsValid_Inclusive_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _inclusiveAttribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not less than or equal to 10"));
    }

    [Test]
    public void IsValid_Inclusive_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateLessThanAttribute(10.0, Inclusion.Inclusive, includeInHealthCheck: false);

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
        Assert.That(script, Contains.Substring("TestProperty.Value <= 10"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be less than or equal to 10"));
    }
}