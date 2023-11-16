using Fig.Web.ExtensionMethods;
using NUnit.Framework;

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
}