using Fig.Web.ExtensionMethods;
using Markdig;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class StringExtensionMethodsTests
{
    [Test]
    public void ShallStripHtmlFromMarkdownDescriptions()
    {
        var markdown = @"
### Hack me

<script>
 alert('gotcha');
</script>
";
        var result = markdown.ToHtml();

        var expectedResult = @"
<p>&lt;script&gt;
alert('gotcha');
&lt;/script&gt;</p>";
        
        Assert.That(result.Contains(expectedResult));
    }
}