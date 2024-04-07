using System.Collections.Generic;
using System.Text;
using Fig.Web.Attributes;
using Fig.Web.ExtensionMethods;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class StringBuilderExtensionMethodTests
{
    [TestCase(1, "# Test Heading")]
    [TestCase(2, "## Test Heading")]
    [TestCase(3, "### Test Heading")]
    [TestCase(4, "#### Test Heading")]
    [TestCase(5, "##### Test Heading")]
    [TestCase(6, "###### Test Heading")]
    public void ShallAppendMarkdownHeading(int headingLevel, string expectedMarkdown)
    {
        // Arrange
        var builder = new StringBuilder();
        
        // Act
        builder.AddHeading(headingLevel, "Test Heading");
        
        // Assert
        Assert.That(expectedMarkdown, Is.EqualTo(builder.ToString().RemoveNewLines()));
    }

    [Test]
    public void ShallAppendMarkdownParagraph()
    {
        // Arrange
        var builder = new StringBuilder();
        
        // Act
        builder.AddParagraph("This is a test paragraph.");
        
        // Assert
        Assert.That("This is a test paragraph.", Is.EqualTo(builder.ToString().RemoveNewLines()));
    }

    [Test]
    public void ShallAppendMarkdownTable_WithSortAttributeDescending()
    {
        // Arrange
        var builder = new StringBuilder();
        var items = new List<SortableOrderedItemDesc>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Alice", Age = 25 }
        };

        // Act
        builder.AddTable(items);

        // Assert
        var expected = "| Age | Name || --- | --- || 30 | John || 25 | Alice |";
        Assert.That(expected, Is.EqualTo(builder.ToString().RemoveNewLines()));
    }

    [Test]
    public void ShallAppendMarkdownTable_WithOrderAndSortAttributes()
    {
        // Arrange
        var builder = new StringBuilder();
        var items = new List<SortableOrderedItem>
        {
            new() { Name = "John", Age = 30 },
            new() { Name = "Alice", Age = 25 }
        };

        // Act
        builder.AddTable(items);

        // Assert
        var expected = "| Age | Name || --- | --- || 25 | Alice || 30 | John |";
        Assert.That(expected, Is.EqualTo(builder.ToString().RemoveNewLines()));
    }

    [Test]
    public void ShallAppendMarkdownProperty()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        builder.AddProperty("Key", "Value");

        // Assert
        Assert.That(builder.ToString().RemoveNewLines(), Is.EqualTo("**Key:** Value"));
    }
}

public class TestItem
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class OrderedItem
{
    [Fig.Web.Attributes.Order(2)]
    public string Name { get; set; }

    [Fig.Web.Attributes.Order(1)]
    public int Age { get; set; }
}

public class SortableOrderedItem
{
    [Fig.Web.Attributes.Order(2)]
    public string Name { get; set; }

    [Fig.Web.Attributes.Order(1)]
    [Sort]
    public int Age { get; set; }
}

public class SortableOrderedItemDesc
{
    [Fig.Web.Attributes.Order(2)]
    public string Name { get; set; }

    [Fig.Web.Attributes.Order(1)]
    [Sort(false)]
    public int Age { get; set; }
}