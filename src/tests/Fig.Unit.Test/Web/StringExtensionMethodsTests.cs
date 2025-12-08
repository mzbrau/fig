using Fig.Web.ExtensionMethods;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class StringExtensionMethodsTests
{
    [Test]
    public void ShallStripHtmlFromMarkdownDescriptions()
    {
        string input = "### Hack me\n\n<script>\n alert('gotcha');\n</script>";
        var result = input.ToHtml();

        string expectedOutput = "<h3 id=\"hack-me\">Hack me</h3>\n<p>&lt;script&gt;\nalert('gotcha');\n&lt;/script&gt;</p>";

        Assert.That(result.Trim('\n'), Is.EqualTo(expectedOutput));
    }

    [Test]
    public void IsValidRegex_ShouldReturnTrue_ForValidRegex()
    {
        // Arrange
        var validPattern = "^[a-zA-Z]+$";

        // Act
        var result = validPattern.IsValidRegex();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsValidRegex_ShouldReturnFalse_ForInvalidRegex()
    {
        // Arrange
        var invalidPattern = "[unclosed";

        // Act
        var result = invalidPattern.IsValidRegex();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValidRegex_ShouldReturnFalse_ForEmptyString()
    {
        // Arrange
        var emptyPattern = "";

        // Act
        var result = emptyPattern.IsValidRegex();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValidRegex_ShouldHandleComplexPatternsWithTimeout()
    {
        // Arrange - a valid regex pattern that's complex but valid
        var complexPattern = "(a+)+$";

        // Act
        var result = complexPattern.IsValidRegex();

        // Assert - the pattern is valid, so it should return true
        // The timeout ensures it doesn't hang if tested against problematic input
        Assert.That(result, Is.True);
    }

    [Test]
    public void SplitCamelCase_ShouldSplitCorrectly()
    {
        // Arrange
        var input = "SomeCamelCaseString";

        // Act
        var result = input.SplitCamelCase();

        // Assert
        Assert.That(result, Is.EqualTo("Some Camel Case String"));
    }

    [Test]
    public void StripImagesAndSimplifyLinks_ShouldRemoveImages()
    {
        // Arrange
        var markdown = "Some text ![alt](image.png) more text";

        // Act
        var result = markdown.StripImagesAndSimplifyLinks();

        // Assert
        Assert.That(result, Does.Not.Contain("!["));
        Assert.That(result, Does.Contain("Some text"));
    }

    [Test]
    public void StripImagesAndSimplifyLinks_ShouldSimplifyLinks()
    {
        // Arrange
        var markdown = "Visit [Google](https://google.com) for search";

        // Act
        var result = markdown.StripImagesAndSimplifyLinks();

        // Assert
        Assert.That(result, Does.Not.Contain("]("));
        Assert.That(result, Does.Contain("Google"));
    }
}