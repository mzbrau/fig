using Fig.Client.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateLessThanAttributeTests
{
    private ValidateLessThanAttribute _attribute = null!;
    private ValidateLessThanAttribute _attributeWithEquals = null!;

    [SetUp]
    public void Setup()
    {
        _attribute = new ValidateLessThanAttribute(10.0);
        _attributeWithEquals = new ValidateLessThanAttribute(10.0, includeEquals: true);
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
    public void IsValid_WithValueLessThanMax_ShouldReturnTrue()
    {
        // Act
        var result = _attribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithValueEqualToMax_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("10 is not less than 10"));
    }

    [Test]
    public void IsValid_WithValueGreaterThanMax_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(15.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("15 is not less than 10"));
    }

    [Test]
    public void IsValid_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not less than 10"));
    }

    [Test]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateLessThanAttribute(10.0, includeInHealthCheck: false);

        // Act
        var result = attribute.IsValid(15.0);

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
        Assert.That(script, Contains.Substring("TestProperty.Value < 10"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be less than 10"));
    }

    [Test]
    public void IsValid_WithIncludeEquals_ValueLessThanMax_ShouldReturnTrue()
    {
        // Act
        var result = _attributeWithEquals.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithIncludeEquals_ValueEqualToMax_ShouldReturnTrue()
    {
        // Act
        var result = _attributeWithEquals.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithIncludeEquals_ValueGreaterThanMax_ShouldReturnFalse()
    {
        // Act
        var result = _attributeWithEquals.IsValid(15.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("15 is not less than or equal to 10"));
    }

    [Test]
    public void IsValid_WithIncludeEquals_NullValue_ShouldReturnFalse()
    {
        // Act
        var result = _attributeWithEquals.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not less than or equal to 10"));
    }

    [Test]
    public void IsValid_WithIncludeEqualsAndHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateLessThanAttribute(10.0, includeInHealthCheck: false, includeEquals: true);

        // Act
        var result = attribute.IsValid(15.0);

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
        Assert.That(script, Contains.Substring("TestProperty.Value <= 10"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be less than or equal to 10"));
    }
}