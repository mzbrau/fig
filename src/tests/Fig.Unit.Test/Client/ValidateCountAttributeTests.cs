using System.Collections.Generic;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidateCountAttributeTests
{
    [Test]
    public void ApplyToTypes_ShouldReturnCorrectTypes()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Exactly, 5);
        
        // Act
        var types = attr.ApplyToTypes;

        // Assert
        Assert.That(types, Contains.Item(typeof(System.Collections.IList)));
        Assert.That(types, Contains.Item(typeof(System.Collections.ICollection)));
        Assert.That(types, Contains.Item(typeof(System.Collections.IEnumerable)));
    }

    [TestCase(Constraint.Exactly, 3, 3, true, "Valid")]
    [TestCase(Constraint.Exactly, 3, 2, false, "Collection has 2 items but must contain exactly 3 items")]
    [TestCase(Constraint.Exactly, 3, 4, false, "Collection has 4 items but must contain exactly 3 items")]
    [TestCase(Constraint.Exactly, 1, 1, true, "Valid")]
    [TestCase(Constraint.Exactly, 1, 0, false, "Collection has 0 items but must contain exactly 1 item")]
    [TestCase(Constraint.AtLeast, 3, 3, true, "Valid")]
    [TestCase(Constraint.AtLeast, 3, 4, true, "Valid")]
    [TestCase(Constraint.AtLeast, 3, 2, false, "Collection has 2 items but must contain at least 3 items")]
    [TestCase(Constraint.AtMost, 3, 3, true, "Valid")]
    [TestCase(Constraint.AtMost, 3, 2, true, "Valid")]
    [TestCase(Constraint.AtMost, 3, 4, false, "Collection has 4 items but must contain at most 3 items")]
    public void IsValid_Various(Constraint condition, int count, int actualItems, bool expectedValid, string expectedMessage)
    {
        // Arrange
        var attr = new ValidateCountAttribute(condition, count);
        var list = new List<string>();
        for (int i = 0; i < actualItems; i++)
        {
            list.Add($"item{i}");
        }
        
        // Act
        var (isValid, msg) = attr.IsValid(list);
        
        // Assert
        Assert.That(isValid, Is.EqualTo(expectedValid));
        Assert.That(msg, Is.EqualTo(expectedMessage));
    }

    [Test]
    public void IsValid_WithNullValue_ShouldReturnFalse()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Exactly, 5);
        
        // Act
        var result = attr.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("Collection is null - Collection must contain exactly 5 items"));
    }

    [Test]
    public void IsValid_WithNonCollectionValue_ShouldReturnFalse()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Exactly, 5);
        
        // Act
        var result = attr.IsValid("not a collection");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("Value is not a collection - Collection must contain exactly 5 items"));
    }

    [Test]
    public void IsValid_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attribute = new ValidateCountAttribute(Constraint.Exactly, 5, includeInHealthCheck: false);
        var list = new List<string> { "item1", "item2" }; // Wrong count

        // Act
        var result = attribute.IsValid(list);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Not validated"));
    }

    [TestCase(Constraint.Exactly, "===", "exactly")]
    [TestCase(Constraint.AtLeast, ">=", "at least")]
    [TestCase(Constraint.AtMost, "<=", "at most")]
    public void GetScript_ShouldReturnCorrectJavaScript(Constraint condition, string expectedOperator, string expectedMessage)
    {
        // Arrange
        var attr = new ValidateCountAttribute(condition, 5);
        
        // Act
        var script = attr.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring($"TestProperty.Value.length {expectedOperator} 5"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring($"Collection must contain {expectedMessage} 5 items"));
    }

    [Test]
    public void GetScript_WithSingularCount_ShouldUseSingularForm()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Exactly, 1);
        
        // Act
        var script = attr.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring("Collection must contain exactly 1 item"));
        Assert.That(script, Does.Not.Contain("1 items")); // Should not have plural form
    }

    [Test]
    public void IsValid_WithEmptyList_ShouldWorkCorrectly()
    {
        // Arrange
        var exactlyZero = new ValidateCountAttribute(Constraint.Exactly, 0);
        var atLeastOne = new ValidateCountAttribute(Constraint.AtLeast, 1);
        var atMostZero = new ValidateCountAttribute(Constraint.AtMost, 0);
        var emptyList = new List<string>();

        // Act & Assert
        var (isValidExactly, _) = exactlyZero.IsValid(emptyList);
        Assert.That(isValidExactly, Is.True);

        var (isValidAtLeast, msgAtLeast) = atLeastOne.IsValid(emptyList);
        Assert.That(isValidAtLeast, Is.False);
        Assert.That(msgAtLeast, Is.EqualTo("Collection has 0 items but must contain at least 1 item"));

        var (isValidAtMost, _) = atMostZero.IsValid(emptyList);
        Assert.That(isValidAtMost, Is.True);
    }

    [Test]
    public void IsValid_WithArray_ShouldWorkCorrectly()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Exactly, 3);
        var array = new string[] { "item1", "item2", "item3" };
        
        // Act
        var (isValid, msg) = attr.IsValid(array);
        
        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(msg, Is.EqualTo("Valid"));
    }

    [Test]
    public void IsValid_WithHashSet_ShouldWorkCorrectly()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.AtLeast, 2);
        var hashSet = new HashSet<string> { "item1", "item2", "item3" };
        
        // Act
        var (isValid, msg) = attr.IsValid(hashSet);
        
        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(msg, Is.EqualTo("Valid"));
    }

    [TestCase(Constraint.Between, 2, 5, 2, true, "Valid")]
    [TestCase(Constraint.Between, 2, 5, 3, true, "Valid")]
    [TestCase(Constraint.Between, 2, 5, 4, true, "Valid")]
    [TestCase(Constraint.Between, 2, 5, 5, true, "Valid")]
    [TestCase(Constraint.Between, 2, 5, 1, false, "Collection has 1 item but must contain between 2 and 5 items (inclusive)")]
    [TestCase(Constraint.Between, 2, 5, 6, false, "Collection has 6 items but must contain between 2 and 5 items (inclusive)")]
    [TestCase(Constraint.Between, 1, 1, 1, true, "Valid")]
    [TestCase(Constraint.Between, 1, 1, 0, false, "Collection has 0 items but must contain between 1 and 1 items (inclusive)")]
    [TestCase(Constraint.Between, 1, 1, 2, false, "Collection has 2 items but must contain between 1 and 1 items (inclusive)")]
    public void IsValid_BetweenConstraint_Various(Constraint condition, int lowerCount, int higherCount, int actualItems, bool expectedValid, string expectedMessage)
    {
        // Arrange
        var attr = new ValidateCountAttribute(condition, lowerCount, higherCount);
        var list = new List<string>();
        for (int i = 0; i < actualItems; i++)
        {
            list.Add($"item{i}");
        }
        
        // Act
        var (isValid, msg) = attr.IsValid(list);
        
        // Assert
        Assert.That(isValid, Is.EqualTo(expectedValid));
        Assert.That(msg, Is.EqualTo(expectedMessage));
    }

    [Test]
    public void Constructor_BetweenConstraint_WithWrongConstructor_NoLongerThrowsInConstructor()
    {
        // Since validation is now deferred to SettingDefinitionFactory, constructor no longer throws
        // Act & Assert - Should not throw in constructor
        Assert.DoesNotThrow(() => new ValidateCountAttribute(Constraint.Between, 5));
    }

    [Test]
    public void Constructor_NonBetweenConstraint_WithBetweenConstructor_NoLongerThrowsInConstructor()
    {
        // Since validation is now deferred to SettingDefinitionFactory, constructor no longer throws
        // Act & Assert - Should not throw in constructor
        Assert.DoesNotThrow(() => new ValidateCountAttribute(Constraint.Exactly, 2, 5));
    }

    [Test]
    public void Constructor_BetweenConstraint_WithInvalidRange_NoLongerThrowsInConstructor()
    {
        // Since validation is now deferred to SettingDefinitionFactory, constructor no longer throws
        // Act & Assert - Should not throw in constructor
        Assert.DoesNotThrow(() => new ValidateCountAttribute(Constraint.Between, 5, 2));
    }

    [Test]
    public void GetScript_BetweenConstraint_ShouldReturnCorrectJavaScript()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Between, 2, 5);
        
        // Act
        var script = attr.GetScript("TestProperty");

        // Assert
        Assert.That(script, Contains.Substring("TestProperty.Value.length >= 2"));
        Assert.That(script, Contains.Substring("TestProperty.Value.length <= 5"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = true"));
        Assert.That(script, Contains.Substring("TestProperty.IsValid = false"));
        Assert.That(script, Contains.Substring("Collection must contain between 2 and 5 items (inclusive)"));
    }

    [Test]
    public void IsValid_BetweenConstraint_WithNullValue_ShouldReturnFalse()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Between, 2, 5);
        
        // Act
        var result = attr.IsValid(null);

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("Collection is null - Collection must contain between 2 and 5 items (inclusive)"));
    }

    [Test]
    public void IsValid_BetweenConstraint_WithNonCollectionValue_ShouldReturnFalse()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Between, 2, 5);
        
        // Act
        var result = attr.IsValid("not a collection");

        // Assert
        Assert.That(result.Item1, Is.False);
        Assert.That(result.Item2, Is.EqualTo("Value is not a collection - Collection must contain between 2 and 5 items (inclusive)"));
    }

    [Test]
    public void IsValid_BetweenConstraint_WithHealthCheckDisabled_ShouldReturnTrue()
    {
        // Arrange
        var attr = new ValidateCountAttribute(Constraint.Between, 2, 5, includeInHealthCheck: false);
        var list = new List<string> { "item1" }; // Wrong count
        
        // Act
        var result = attr.IsValid(list);

        // Assert
        Assert.That(result.Item1, Is.True);
        Assert.That(result.Item2, Is.EqualTo("Not validated"));
    }

    [Test]
    public void IsValid_BetweenConstraint_WithEmptyList_ShouldWorkCorrectly()
    {
        // Arrange
        var betweenZeroTwo = new ValidateCountAttribute(Constraint.Between, 0, 2);
        var betweenOneThree = new ValidateCountAttribute(Constraint.Between, 1, 3);
        var emptyList = new List<string>();

        // Act & Assert
        var (isValidZeroTwo, _) = betweenZeroTwo.IsValid(emptyList);
        Assert.That(isValidZeroTwo, Is.True);

        var (isValidOneThree, msgOneThree) = betweenOneThree.IsValid(emptyList);
        Assert.That(isValidOneThree, Is.False);
        Assert.That(msgOneThree, Is.EqualTo("Collection has 0 items but must contain between 1 and 3 items (inclusive)"));
    }
}