using Fig.Client.Description;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class MarkdownExtractorTests
{
    private const string MarkdownDocument = @"# Heading1
heading1 content
## SubHeading1
subheading1 content
## SubHeading2
subheading2 content
### SubSubHeading1
subsubheading1 content
# Heading2
heading2 content";
    
    [Test]
    public void ShallExtractContentUnderHeading()
    {
        var sut = new MarkdownExtractor();
        var result = sut.ExtractSection(MarkdownDocument, "Heading2");
        Assert.That(result, Is.EqualTo("heading2 content"));
    }

    [Test]
    public void ShallExtractContentUnderSubHeading()
    {
        var sut = new MarkdownExtractor();
        var result = sut.ExtractSection(MarkdownDocument, "SubHeading1");
        Assert.That(result, Is.EqualTo("subheading1 content"));
    }

    [Test]
    public void ShallReturnEmptyStringIfHeadingIsNotFound()
    {
        var sut = new MarkdownExtractor();
        var result = sut.ExtractSection(MarkdownDocument, "HeadingDoesNotExist");
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ShallReturnSubHeadingsUnderSelectedHeading()
    {
        var sut = new MarkdownExtractor();
        var result = sut.ExtractSection(MarkdownDocument, "Heading1");
        Assert.That(result, Is.EqualTo(@"heading1 content

## SubHeading1

subheading1 content

## SubHeading2

subheading2 content

### SubSubHeading1

subsubheading1 content".Replace("\r", string.Empty))); // Note additional spacing is due to markdown formatting.
    }
}