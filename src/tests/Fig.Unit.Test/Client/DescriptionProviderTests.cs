using System;
using System.Collections.Generic;
using System.Text;
using Fig.Client.Description;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class DescriptionProviderTests
{
    private Mock<IInternalResourceProvider> _internalResourceProviderMock = null!;
    private Mock<IMarkdownExtractor> _markdownExtractorMock = null!;
    private DescriptionProvider _descriptionProvider = null!;

    [SetUp]
    public void Setup()
    {
        _internalResourceProviderMock = new Mock<IInternalResourceProvider>();
        _markdownExtractorMock = new Mock<IMarkdownExtractor>();
        _descriptionProvider = new DescriptionProvider(_internalResourceProviderMock.Object, _markdownExtractorMock.Object);
    }

    [Test]
    public void GetDescription_WhenDescriptionDoesNotStartWithDollar_ShouldReturnDescriptionAsIs()
    {
        // Arrange
        const string description = "This is a regular description";

        // Act
        var result = _descriptionProvider.GetDescription(description);

        // Assert
        Assert.That(result, Is.EqualTo(description));
        _internalResourceProviderMock.VerifyNoOtherCalls();
        _markdownExtractorMock.VerifyNoOtherCalls();
    }

    [Test]
    public void GetDescription_WhenDescriptionStartsWithDollar_ShouldProcessResourceKey()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string markdownContent = "# Test Header\nThis is test content";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownContent);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result, Contains.Substring("This is test content"));
        _internalResourceProviderMock.Verify(x => x.GetStringResource("test-resource"), Times.Once);
    }

    [Test]
    public void GetDescription_WhenResourceKeyContainsSection_ShouldExtractSection()
    {
        // Arrange
        const string resourceKey = "$test-resource#section1";
        const string markdownContent = "# Test Header\nThis is test content";
        const string extractedSection = "This is extracted section content";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownContent);
        _markdownExtractorMock.Setup(x => x.ExtractSection(markdownContent, "section1"))
            .Returns(extractedSection);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result, Contains.Substring(extractedSection));
        _internalResourceProviderMock.Verify(x => x.GetStringResource("test-resource"), Times.Once);
        _markdownExtractorMock.Verify(x => x.ExtractSection(markdownContent, "section1"), Times.Once);
    }

    [Test]
    public void GetDescription_WhenMultipleResourceKeys_ShouldCombineWithDividers()
    {
        // Arrange
        const string resourceKeys = "$resource1, $resource2, $resource3";
        const string content1 = "Content 1";
        const string content2 = "Content 2";
        const string content3 = "Content 3";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource1")).Returns(content1);
        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource2")).Returns(content2);
        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource3")).Returns(content3);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKeys);

        // Assert
        Assert.That(result, Contains.Substring(content1));
        Assert.That(result, Contains.Substring(content2));
        Assert.That(result, Contains.Substring(content3));
        Assert.That(result, Contains.Substring("---")); // Divider should be present
        _internalResourceProviderMock.Verify(x => x.GetStringResource("resource1"), Times.Once);
        _internalResourceProviderMock.Verify(x => x.GetStringResource("resource2"), Times.Once);
        _internalResourceProviderMock.Verify(x => x.GetStringResource("resource3"), Times.Once);
    }

    [Test]
    public void GetDescription_WhenResourceNotFound_ShouldReturnResourceKeyAndLogError()
    {
        // Arrange
        const string resourceKey = "$non-existent-resource";
        
        _internalResourceProviderMock.Setup(x => x.GetStringResource("non-existent-resource"))
            .Throws(new Exception("Resource not found"));

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = _descriptionProvider.GetDescription(resourceKey);

            // Assert
            Assert.That(result, Contains.Substring(resourceKey));
            var consoleOutput = stringWriter.ToString();
            Assert.That(consoleOutput, Contains.Substring("Error while trying to read or process resource"));
            Assert.That(consoleOutput, Contains.Substring(resourceKey));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void GetDescription_WhenEmptyResourceContent_ShouldNotAddDivider()
    {
        // Arrange
        const string resourceKeys = "$resource1, $resource2";
        const string content1 = "Content 1";
        const string content2 = ""; // Empty content

        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource1")).Returns(content1);
        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource2")).Returns(content2);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKeys);

        // Assert
        Assert.That(result, Contains.Substring(content1));
        // Should not contain divider since second resource is empty
        Assert.That(result.Split("---").Length, Is.EqualTo(1));
    }

    [Test]
    public void GetAllMarkdownResourceKeys_ShouldDelegateToInternalResourceProvider()
    {
        // Arrange
        var expectedKeys = new List<string> { "key1", "key2", "key3" };
        _internalResourceProviderMock.Setup(x => x.GetAllMarkdownResourceKeys()).Returns(expectedKeys);

        // Act
        var result = _descriptionProvider.GetAllMarkdownResourceKeys();

        // Assert
        Assert.That(result, Is.EqualTo(expectedKeys));
        _internalResourceProviderMock.Verify(x => x.GetAllMarkdownResourceKeys(), Times.Once);
    }

    [Test]
    public void ProcessMarkdown_ShouldStripFrontMatter()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string markdownWithFrontMatter = @"---
title: Test Document
author: Test Author
---

# Content
This is the actual content.";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithFrontMatter);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result, Does.Not.Contain("title: Test Document"));
        Assert.That(result, Does.Not.Contain("author: Test Author"));
        Assert.That(result, Contains.Substring("This is the actual content"));
    }

    [Test]
    public void ProcessMarkdown_ShouldStripInternalAdmonitions()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string markdownWithInternalAdmonition = @"# Content
This is public content.
:::internal
This is internal content that should be stripped.
:::
More public content.";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithInternalAdmonition);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result, Does.Not.Contain("This is internal content"));
        Assert.That(result, Contains.Substring("This is public content"));
        Assert.That(result, Contains.Substring("More public content"));
    }

    [Test]
    public void ProcessMarkdown_ShouldStripFigExcludeAdmonitions()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string markdownWithFigExclude = @"# Content
This is included content.
:::figexclude
This content should be excluded from Fig.
:::
More included content.";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithFigExclude);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result, Does.Not.Contain("This content should be excluded"));
        Assert.That(result, Contains.Substring("This is included content"));
        Assert.That(result, Contains.Substring("More included content"));
    }

    [Test]
    public void ProcessMarkdown_ShouldConvertInternalLinksToMarkdownBold()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string markdownWithInternalLinks = @"# Content
Check out [this internal link](internal-page) for more info.
Also see [another link](../other-page.md) here.
But [this external link](https://example.com) should remain unchanged.";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithInternalLinks);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result, Contains.Substring("**this internal link**"));
        Assert.That(result, Contains.Substring("**another link**"));
        Assert.That(result, Contains.Substring("[this external link](https://example.com)"));
    }

    [Test]
    public void ProcessMarkdown_ShouldEmbedPngImagesAsBase64()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string markdownWithImage = @"# Content
Here is an image:
![Test Image](test-image.png)
More content.";
        
        var imageBytes = Encoding.UTF8.GetBytes("fake png data");
        
        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithImage);
        _internalResourceProviderMock.Setup(x => x.GetImageResourceBytes("test-image.png"))
            .Returns(imageBytes);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result, Contains.Substring("data:image/png;base64,"));
        Assert.That(result, Does.Not.Contain("test-image.png"));
        _internalResourceProviderMock.Verify(x => x.GetImageResourceBytes("test-image.png"), Times.Once);
    }

    [Test]
    public void ProcessMarkdown_ShouldEmbedSvgImagesAsBase64()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string markdownWithSvg = @"# Content
Here is an SVG:
![Test SVG](test-image.svg)
More content.";
        
        var svgContent = "<svg><rect width='100' height='100'/></svg>";
        var svgBytes = Encoding.UTF8.GetBytes(svgContent);
        
        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithSvg);
        _internalResourceProviderMock.Setup(x => x.GetImageResourceBytes("test-image.svg"))
            .Returns(svgBytes);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result, Contains.Substring("data:image/svg+xml;base64,"));
        Assert.That(result, Does.Not.Contain("test-image.svg"));
        _internalResourceProviderMock.Verify(x => x.GetImageResourceBytes("test-image.svg"), Times.Once);
    }

    [Test]
    public void ProcessMarkdown_WhenImageNotFound_ShouldKeepOriginalImageName()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string markdownWithImage = @"# Content
![Missing Image](missing-image.png)
More content.";
        
        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithImage);
        _internalResourceProviderMock.Setup(x => x.GetImageResourceBytes("missing-image.png"))
            .Returns((byte[]?)null);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        // When image is not found, the filename is returned but the internal link regex
        // will convert the markdown image syntax to bold text
        Assert.That(result, Contains.Substring("**Missing Image**"));
        Assert.That(result, Does.Not.Contain("data:image/"));
    }

    [Test]
    public void ProcessMarkdown_WithExistingDataUrl_ShouldProcessAndConvertToBold()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string base64Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";
        const string dataUrl = $"data:image/png;base64,{base64Data}";
        const string markdownWithDataUrl = $@"# Content
![Existing Base64]({dataUrl})
More content.";
        
        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithDataUrl);
        // Path.GetFileName on the data URL extracts the last part which is part of the base64 string
        _internalResourceProviderMock.Setup(x => x.GetImageResourceBytes("PchI7wAAAABJRU5ErkJggg=="))
            .Returns((byte[]?)null);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        // Data URLs get processed by EmbedImages first, then by RemoveInternalLinks
        // which converts the image syntax to bold text
        Assert.That(result, Contains.Substring("!**Existing Base64**"));
        _internalResourceProviderMock.Verify(x => x.GetImageResourceBytes("PchI7wAAAABJRU5ErkJggg=="), Times.Once);
    }

    [Test]
    public void ProcessMarkdown_WithPureBase64String_ShouldSkipImageProcessing()
    {
        // Arrange
        const string resourceKey = "$test-resource";
        const string pureBase64 = "aWVBQkNERUZHSEk="; // Valid base64 string (length % 4 == 0, valid chars)
        const string markdownWithPureBase64 = $@"# Content
![Pure Base64]({pureBase64})
More content.";
        
        _internalResourceProviderMock.Setup(x => x.GetStringResource("test-resource"))
            .Returns(markdownWithPureBase64);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        // Pure base64 strings pass the IsBase64String check so they don't get processed by EmbedImages,
        // but they can still be processed by RemoveInternalLinks since they're not URLs with protocols.
        // The RemoveInternalLinks regex will convert ![text](url) to !**text** when url doesn't match the protocols
        Assert.That(result, Contains.Substring("!**Pure Base64**"));
        _internalResourceProviderMock.Verify(x => x.GetImageResourceBytes(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void ProcessMarkdown_ShouldHandleComplexMarkdownWithAllFeatures()
    {
        // Arrange
        const string resourceKey = "$complex-resource";
        const string complexMarkdown = @"---
title: Complex Document
---

# Main Heading

This is content with [internal link](internal-page) and ![image](test.png).

:::internal
Internal content to remove
:::

:::figexclude
Fig-specific content to remove
:::

More content with [external link](https://example.com).";

        var imageBytes = Encoding.UTF8.GetBytes("fake image data");
        
        _internalResourceProviderMock.Setup(x => x.GetStringResource("complex-resource"))
            .Returns(complexMarkdown);
        _internalResourceProviderMock.Setup(x => x.GetImageResourceBytes("test.png"))
            .Returns(imageBytes);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        // Should strip front matter
        Assert.That(result, Does.Not.Contain("title: Complex Document"));
        
        // Should convert internal link to bold
        Assert.That(result, Contains.Substring("**internal link**"));
        
        // Should embed image as base64
        Assert.That(result, Contains.Substring("data:image/png;base64,"));
        
        // Should preserve external link
        Assert.That(result, Contains.Substring("[external link](https://example.com)"));
        
        // Should strip admonitions
        Assert.That(result, Does.Not.Contain("Internal content to remove"));
        Assert.That(result, Does.Not.Contain("Fig-specific content to remove"));
        
        // Should preserve other content
        Assert.That(result, Contains.Substring("Main Heading"));
        Assert.That(result, Contains.Substring("More content"));
    }

    [Test]
    public void GetDescription_WhenResourceKeyHasWhitespace_ShouldTrimCorrectly()
    {
        // Arrange
        const string resourceKeys = "$resource1, $resource2, $resource3";
        const string content1 = "Content 1";
        const string content2 = "Content 2";
        const string content3 = "Content 3";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource1")).Returns(content1);
        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource2")).Returns(content2);
        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource3")).Returns(content3);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKeys);

        // Assert
        Assert.That(result, Contains.Substring(content1));
        Assert.That(result, Contains.Substring(content2));
        Assert.That(result, Contains.Substring(content3));
        _internalResourceProviderMock.Verify(x => x.GetStringResource("resource1"), Times.Once);
        _internalResourceProviderMock.Verify(x => x.GetStringResource("resource2"), Times.Once);
        _internalResourceProviderMock.Verify(x => x.GetStringResource("resource3"), Times.Once);
    }

    [Test]
    public void GetDescription_WhenDescriptionHasLeadingWhitespace_ShouldReturnAsIs()
    {
        // Arrange
        const string descriptionWithWhitespace = " $resource1 , $resource2 , $resource3 ";

        // Act
        var result = _descriptionProvider.GetDescription(descriptionWithWhitespace);

        // Assert
        Assert.That(result, Is.EqualTo(descriptionWithWhitespace));
        _internalResourceProviderMock.VerifyNoOtherCalls();
        _markdownExtractorMock.VerifyNoOtherCalls();
    }

    [Test]
    public void GetDescription_WhenSingleEmptyResource_ShouldReturnEmpty()
    {
        // Arrange
        const string resourceKey = "$empty-resource";
        const string emptyContent = "";

        _internalResourceProviderMock.Setup(x => x.GetStringResource("empty-resource"))
            .Returns(emptyContent);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKey);

        // Assert
        Assert.That(result.Trim(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetDescription_WhenOnlyWhitespaceResources_ShouldNotAddDividers()
    {
        // Arrange
        const string resourceKeys = "$resource1, $resource2";
        const string content1 = "   "; // Only whitespace
        const string content2 = "\n\t"; // Only whitespace

        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource1")).Returns(content1);
        _internalResourceProviderMock.Setup(x => x.GetStringResource("resource2")).Returns(content2);

        // Act
        var result = _descriptionProvider.GetDescription(resourceKeys);

        // Assert
        Assert.That(result, Does.Not.Contain("---"));
    }
}
