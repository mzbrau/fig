using Fig.Client.Attributes;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateIsBetweenAttributeTests
{
    private ValidateIsBetweenAttribute _attribute;

    [SetUp]
    public void Setup()
    {
        _attribute = new ValidateIsBetweenAttribute(5.0, 10.0);
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
    public void IsValid_WithValueInRange_ShouldReturnTrue()
    {
        // Act
        var result = _attribute.IsValid(7.5);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithValueAtLowerBound_ShouldReturnTrue()
    {
        // Act
        var result = _attribute.IsValid(5.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithValueAtUpperBound_ShouldReturnTrue()
    {
        // Act
        var result = _attribute.IsValid(10.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithValueBelowRange_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("3 is not between 5 and 10"));
    }

    [Test]
    public void IsValid_WithValueAboveRange_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(15.0);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("15 is not between 5 and 10"));
    }

    [Test]
    public void IsValid_WithNullValue_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(" is not between 5 and 10"));
    }

    [Test]
    public void IsValid_WithIntegerValue_ShouldWork()
    {
        // Act
        var result = _attribute.IsValid(8);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithLongValue_ShouldWork()
    {
        // Act
        var result = _attribute.IsValid(9L);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithNonNumericType_ShouldReturnFalse()
    {
        // Act
        var result = _attribute.IsValid("not a number");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("not a number is not between 5 and 10"));
    }

    [Test]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateIsBetweenAttribute(5.0, 10.0, includeInHealthCheck: false);

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
        Assert.That(script, Contains.Substring("TestProperty.Value > 5"));
        Assert.That(script, Contains.Substring("TestProperty.Value < 10"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("TestProperty must be between 5 and 10"));
    }
}