using Fig.Web.ExtensionMethods;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class StringExtensionMethodsTests
{
    [TearDown]
    public void TearDown()
    {
        StringExtensionMethods.TakeDescriptionHtmlElapsedMs();
    }

    [Test]
    public void ShallStripHtmlFromMarkdownDescriptions()
    {
        string input = "### Hack me\n\n<script>\n alert('gotcha');\n</script>";
        var result = input.ToHtml();

        string expectedOutput = "<h3 id=\"hack-me\">Hack me</h3>\n<p>&lt;script&gt;\nalert('gotcha');\n&lt;/script&gt;</p>";

        Assert.That(result.Trim('\n'), Is.EqualTo(expectedOutput));
    }

    [Test]
    public void ToHtml_EmptyString_ReturnsEmptyWithoutMarkdown()
    {
        StringExtensionMethods.ResetDescriptionHtmlTiming();
        Assert.That(string.Empty.ToHtml(), Is.Empty);
    }

    [Test]
    public void ToHtml_PlainText_HtmlEncodesWithoutMarkdigStructure()
    {
        var result = "Hello world & friends".ToHtml();

        Assert.That(result, Is.EqualTo("Hello world &amp; friends"));
        Assert.That(result, Does.Not.Contain("<p>"));
    }

    [Test]
    public void LooksLikeMarkdown_DetectsCommonMarkers()
    {
        Assert.That(StringExtensionMethods.LooksLikeMarkdown("## Title"), Is.True);
        Assert.That(StringExtensionMethods.LooksLikeMarkdown("use `code`"), Is.True);
        Assert.That(StringExtensionMethods.LooksLikeMarkdown("plain hyphenated-word"), Is.False);
    }

    [Test]
    public void SplitCamelCase_InsertsSpacesWithoutRegex()
    {
        Assert.That("MySettingName".SplitCamelCase(), Is.EqualTo("My Setting Name"));
        Assert.That("XMLParser".SplitCamelCase(), Is.EqualTo("XML Parser"));
        Assert.That("a".SplitCamelCase(), Is.EqualTo("a"));
        Assert.That(string.Empty.SplitCamelCase(), Is.EqualTo(string.Empty));
    }
}