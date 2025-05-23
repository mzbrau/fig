using System.Collections.Generic;
using System.Linq;
using NUnit.Framework; // Replaced Xunit

namespace Fig.Unit.Test.Web
{
    [TestFixture] // Added TestFixture
    public class DataGridCsvHelperTests
    {
        [Test] // Changed from [Theory]
        [TestCase("a,b,c", new[] { "a", "b", "c" })] // Changed from [InlineData]
        [TestCase("\"a,b\",c", new[] { "a,b", "c" })]
        [TestCase("\"a \"\"b\"\" c\",d", new[] { "a \"b\" c", "d" })]
        [TestCase("a,,c", new[] { "a", "", "c" })]
        [TestCase("\"\",b,\"\"", new[] { "", "b", "" })]
        [TestCase("\"a\",b,\"c,d\"", new[] { "a", "b", "c,d" })]
        [TestCase(" a , b , c ", new[] { " a ", " b ", " c " })] 
        [TestCase("\" a \",\" b \",\" c \"", new[] { " a ", " b ", " c " })]
        public void ParseCsvLine_VariousInputs_ReturnsCorrectFields(string line, string[] expected)
        {
            var result = CsvTestHelpers.ParseCsvLine(line);
            Assert.That(result, Is.EqualTo(expected)); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void ParseCsvLine_EmptyLine_ReturnsOneEmptyField()
        {
            var result = CsvTestHelpers.ParseCsvLine("");
            Assert.That(result, Is.EqualTo(new List<string> { "" })); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void ParseCsvLine_NullLine_ReturnsEmptyList()
        {
            var result = CsvTestHelpers.ParseCsvLine(null);
            Assert.That(result, Is.Empty); // NUnit constraint-based
        }
        
        [Test] // Changed from [Theory]
        [TestCase(",,", new[] { "", "", "" })] // Changed from [InlineData]
        [TestCase(" , , ", new[] { " ", " ", " " })] 
        [TestCase("\"\",\"\",\"\"", new[] { "", "", "" })]
        public void ParseCsvLine_LineWithOnlyCommasOrEmptyQuotedFields_ReturnsCorrectFields(string line, string[] expected)
        {
            var result = CsvTestHelpers.ParseCsvLine(line);
            Assert.That(result, Is.EqualTo(expected)); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void ParseCsvLine_LineWithTrailingComma_IncludesLastEmptyField()
        {
            Assert.That(CsvTestHelpers.ParseCsvLine("a,b,"), Is.EqualTo(new List<string> { "a", "b", "" })); // NUnit constraint-based
            Assert.That(CsvTestHelpers.ParseCsvLine("a,,"), Is.EqualTo(new List<string> { "a", "", "" })); // NUnit constraint-based
            Assert.That(CsvTestHelpers.ParseCsvLine(",,"), Is.EqualTo(new List<string> { "", "", "" })); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void ParseCsvLine_SingleField_NoCommas()
        {
            Assert.That(CsvTestHelpers.ParseCsvLine("abc"), Is.EqualTo(new List<string> { "abc" })); // NUnit constraint-based
            Assert.That(CsvTestHelpers.ParseCsvLine("\"abc\""), Is.EqualTo(new List<string> { "abc" }));  // NUnit constraint-based
        }
        
        [Test] // Changed from [Fact]
        public void ParseCsvLine_SingleQuotedFieldWithInternalQuotes()
        {
            Assert.That(CsvTestHelpers.ParseCsvLine("\"a \"\"b\"\" c\""), Is.EqualTo(new List<string> { "a \"b\" c" })); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void ParseCsvLine_EndsWithEscapedQuote()
        {
             Assert.That(CsvTestHelpers.ParseCsvLine("\"field with quote at end \"\"\""), Is.EqualTo(new List<string> { "field with quote at end \"" })); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void ParseCsvLine_FieldStartsWithSpaceThenQuote()
        {
            // This is tricky based on CSV standards. If a field starts with whitespace then a quote,
            // it's often treated as an unquoted field.
            // Example: `  "a",b` might be `["  \"a\"", "b"]` or an error.
            // The current parser: if (c == '"' && currentField.Length == 0) inQuotes = true;
            // So if currentField is not empty (e.g. contains spaces), it won't enter quoted mode.
            Assert.That(CsvTestHelpers.ParseCsvLine("  \"a\",b"), Is.EqualTo(new List<string> { "  \"a\"", "b" })); // NUnit constraint-based
        }

        [Test] // Changed from [Fact]
        public void ParseCsvLine_UnmatchedQuote()
        {
            Assert.That(CsvTestHelpers.ParseCsvLine("a\"b,c"), Is.EqualTo(new List<string> { "a\"b", "c" })); // NUnit constraint-based
            Assert.That(CsvTestHelpers.ParseCsvLine("\"a,b\"c\""), Is.EqualTo(new List<string> { "a,bc\"" })); // NUnit constraint-based
            Assert.That(CsvTestHelpers.ParseCsvLine("a,\"\"b\""), Is.EqualTo(new List<string> { "a", "\"b" })); // NUnit constraint-based
            Assert.That(CsvTestHelpers.ParseCsvLine("\"a,b,c"), Is.EqualTo(new List<string> { "a,b,c" })); // NUnit constraint-based
        }
        
        [Test] // Changed from [Fact]
        public void ParseCsvLine_EmptyStringNotNull_ReturnsListWithOneEmptyString()
        {
            var result = CsvTestHelpers.ParseCsvLine(""); 
            Assert.That(result, Has.Count.EqualTo(1)); // NUnit constraint-based
            Assert.That(result[0], Is.EqualTo(""));    // NUnit constraint-based
        }
    }
}
