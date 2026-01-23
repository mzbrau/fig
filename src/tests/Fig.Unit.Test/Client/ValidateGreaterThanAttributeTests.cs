using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateGreaterThanAttributeTests
{

    [Test]
    public void ApplyToTypes_ShouldReturnCorrectTypes()
    {
        // Arrange
        var attr = new ValidateGreaterThanAttribute(5.0, Inclusion.Exclusive);
        
        // Act
        var types = attr.ApplyToTypes;

        // Assert
        Assert.That(types, Contains.Item(typeof(double)));
        Assert.That(types, Contains.Item(typeof(int)));
        Assert.That(types, Contains.Item(typeof(long)));
    }

    [TestCase(10.0, Inclusion.Exclusive, true, "Valid")]
    [TestCase(5.0, Inclusion.Exclusive, false, "5 is not greater than 5")]
    [TestCase(3.0, Inclusion.Exclusive, false, "3 is not greater than 5")]
    [TestCase(10.0, Inclusion.Inclusive, true, "Valid")]
    [TestCase(5.0, Inclusion.Inclusive, true, "Valid")]
    [TestCase(3.0, Inclusion.Inclusive, false, "3 is not greater than or equal to 5")]
    [TestCase(5.5, Inclusion.Exclusive, true, "Valid")] // decimal coverage
    [TestCase(4.5, Inclusion.Inclusive, false, "4.5 is not greater than or equal to 5")] // decimal coverage
    public void IsValid_Various(double value, Inclusion inclusion, bool expectedValid, string expectedMessage)
    {
        // Arrange
        var attr = new ValidateGreaterThanAttribute(5.0, inclusion);
        
        // Act
        var (isValid, msg) = attr.IsValid(value);
        
        // Assert
        Assert.That(isValid, Is.EqualTo(expectedValid));
        Assert.That(msg, Is.EqualTo(expectedMessage));
    }

    [TestCase(Inclusion.Exclusive, " is not greater than 5")]
    [TestCase(Inclusion.Inclusive, " is not greater than or equal to 5")]
    public void IsValid_WithNullValue_ShouldReturnFalse(Inclusion inclusion, string expectedMessage)
    {
        // Arrange
        var attr = new ValidateGreaterThanAttribute(5.0, inclusion);
        
        // Act
        var result = attr.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo(expectedMessage));
    }

    [TestCase(Inclusion.Exclusive)]
    [TestCase(Inclusion.Inclusive)]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue(Inclusion inclusion)
    {
        // Arrange
        var attribute = new ValidateGreaterThanAttribute(5.0, inclusion, includeInHealthCheck: false);

        // Act
        var result = attribute.IsValid(3.0);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Not validated"));
    }

    [TestCase(Inclusion.Exclusive, "> 5", "greater than 5")]
    [TestCase(Inclusion.Inclusive, ">= 5", "greater than or equal to 5")]
    public void GetScript_ShouldReturnCorrectJavaScript(Inclusion inclusion, string expectedOperator, string expectedMessage)
    {
        // Arrange
        var attr = new ValidateGreaterThanAttribute(5.0, inclusion);
        
        // Act
        var script = attr.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring($"TestProperty.Value {expectedOperator}"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring($"TestProperty must be {expectedMessage}"));
    }

    [TestCase(5, Inclusion.Inclusive, true, "Valid")]
    [TestCase(5L, Inclusion.Inclusive, true, "Valid")]
    [TestCase(4, Inclusion.Inclusive, false, "4 is not greater than or equal to 5")]
    [TestCase(4L, Inclusion.Exclusive, false, "4 is not greater than 5")]
    public void IsValid_IntegerTypes_ShouldWorkCorrectly(object value, Inclusion inclusion, bool expectedValid, string expectedMessage)
    {
        // Arrange
        var attr = new ValidateGreaterThanAttribute(5.0, inclusion);
        
        // Act
        var result = attr.IsValid(value);
        
        // Assert
        Assert.That(result.Item1, Is.EqualTo(expectedValid));
        if (!expectedValid)
        {
            Assert.That(result.Item2, Is.EqualTo(expectedMessage));
        }
    }
}