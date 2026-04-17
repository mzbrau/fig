using Fig.Client.Abstractions.Validation;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

public class DisplayScriptPlaceholderTests
{
    [Test]
    public void SubstitutePlaceholder_ReplacesPlaceholder_ForFlatSetting()
    {
        // Arrange
        var script = "{{this}}.Value = 5;";

        // Act
        var result = DisplayScriptPath.SubstitutePlaceholder(script, "MySetting");

        // Assert
        Assert.That(result, Is.EqualTo("MySetting.Value = 5;"));
    }

    [Test]
    public void SubstitutePlaceholder_ReplacesPlaceholder_ForNestedSetting()
    {
        // Arrange
        var script = "{{this}}.Value = 'localhost';";

        // Act
        var result = DisplayScriptPath.SubstitutePlaceholder(script, "Connection->Host");

        // Assert
        Assert.That(result, Is.EqualTo("Connection.Host.Value = 'localhost';"));
    }

    [Test]
    public void SubstitutePlaceholder_ReplacesMultiplePlaceholders()
    {
        // Arrange
        var script = "if ({{this}}.Value > 0) { {{this}}.IsValid = true; } else { {{this}}.IsValid = false; }";

        // Act
        var result = DisplayScriptPath.SubstitutePlaceholder(script, "Port");

        // Assert
        Assert.That(result, Is.EqualTo("if (Port.Value > 0) { Port.IsValid = true; } else { Port.IsValid = false; }"));
    }

    [Test]
    public void SubstitutePlaceholder_NoPlaceholder_LeavesScriptUnchanged()
    {
        // Arrange
        var script = "MySetting.Value = 42;";

        // Act
        var result = DisplayScriptPath.SubstitutePlaceholder(script, "MySetting");

        // Assert
        Assert.That(result, Is.EqualTo("MySetting.Value = 42;"));
    }

    [Test]
    public void SubstitutePlaceholder_NullScript_ReturnsNull()
    {
        // Act
        var result = DisplayScriptPath.SubstitutePlaceholder(null, "MySetting");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void SubstitutePlaceholder_EmptyScript_ReturnsEmpty()
    {
        // Act
        var result = DisplayScriptPath.SubstitutePlaceholder(string.Empty, "MySetting");

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void SubstitutePlaceholder_DeeplyNestedSetting()
    {
        // Arrange
        var script = "{{this}}.IsValid = true;";

        // Act
        var result = DisplayScriptPath.SubstitutePlaceholder(script, "A->B->C->D");

        // Assert
        Assert.That(result, Is.EqualTo("A.B.C.D.IsValid = true;"));
    }
}
