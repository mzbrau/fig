using Fig.Web.Models.Setting;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingFilterParserTests
{
    private SettingFilterParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        _parser = new SettingFilterParser();
    }

    [Test]
    public void Parse_EmptyString_ReturnsEmptyCriteria()
    {
        var result = _parser.Parse("");
        
        Assert.That(result.IsEmpty, Is.True);
    }

    [Test]
    public void Parse_AdvancedTrue_SetsAdvancedFilter()
    {
        var result = _parser.Parse("advanced:true");
        
        Assert.That(result.Advanced, Is.True);
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_AdvancedFalse_SetsAdvancedFilter()
    {
        var result = _parser.Parse("advanced:false");
        
        Assert.That(result.Advanced, Is.False);
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_CategoryFilter_SetsCategoryFilter()
    {
        var result = _parser.Parse("category:database");
        
        Assert.That(result.Category, Is.EqualTo("database"));
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_SecretTrue_SetsSecretFilter()
    {
        var result = _parser.Parse("secret:true");
        
        Assert.That(result.Secret, Is.True);
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_ValidFalse_SetsValidFilter()
    {
        var result = _parser.Parse("valid:false");
        
        Assert.That(result.Valid, Is.False);
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_ModifiedTrue_SetsModifiedFilter()
    {
        var result = _parser.Parse("modified:true");
        
        Assert.That(result.Modified, Is.True);
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_GeneralSearchTerm_AddsToGeneralSearchTerms()
    {
        var result = _parser.Parse("database");
        
        Assert.That(result.GeneralSearchTerms, Has.Count.EqualTo(1));
        Assert.That(result.GeneralSearchTerms[0], Is.EqualTo("database"));
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_MultipleGeneralSearchTerms_AddsAll()
    {
        var result = _parser.Parse("database connection string");
        
        Assert.That(result.GeneralSearchTerms, Has.Count.EqualTo(3));
        Assert.That(result.GeneralSearchTerms, Does.Contain("database"));
        Assert.That(result.GeneralSearchTerms, Does.Contain("connection"));
        Assert.That(result.GeneralSearchTerms, Does.Contain("string"));
    }

    [Test]
    public void Parse_MixedFiltersAndSearchTerms_ParsesCorrectly()
    {
        var result = _parser.Parse("advanced:true database category:api");
        
        Assert.That(result.Advanced, Is.True);
        Assert.That(result.Category, Is.EqualTo("api"));
        Assert.That(result.GeneralSearchTerms, Has.Count.EqualTo(1));
        Assert.That(result.GeneralSearchTerms[0], Is.EqualTo("database"));
    }

    [Test]
    public void Parse_CaseInsensitivePrefixes_ParsesCorrectly()
    {
        var result = _parser.Parse("ADVANCED:true Category:API secret:TRUE");
        
        Assert.That(result.Advanced, Is.True);
        Assert.That(result.Category, Is.EqualTo("API"));
        Assert.That(result.Secret, Is.True);
    }

    [Test]
    public void Parse_ComplexFilter_ParsesAllComponents()
    {
        var result = _parser.Parse("modified:true valid:false database connection");
        
        Assert.That(result.Modified, Is.True);
        Assert.That(result.Valid, Is.False);
        Assert.That(result.GeneralSearchTerms, Has.Count.EqualTo(2));
        Assert.That(result.GeneralSearchTerms, Does.Contain("database"));
        Assert.That(result.GeneralSearchTerms, Does.Contain("connection"));
    }

    [Test]
    public void Parse_InvalidBooleanValue_IgnoresFilter()
    {
        var result = _parser.Parse("advanced:notabool");
        
        Assert.That(result.Advanced, Is.Null);
        Assert.That(result.IsEmpty, Is.True);
    }

    [Test]
    public void Parse_TextBeforeColon_TreatedAsGeneralSearch()
    {
        var result = _parser.Parse("search database:connection");
        
        // Both "search" and "database:connection" are treated as general search terms
        // since "database" is not a valid filter prefix
        Assert.That(result.GeneralSearchTerms, Has.Count.EqualTo(2));
        Assert.That(result.GeneralSearchTerms, Does.Contain("search"));
        Assert.That(result.GeneralSearchTerms, Does.Contain("database:connection"));
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_CategoryWithSpaces_CapturesValueAfterColon()
    {
        // Note: This will only capture the first word after colon since we split by spaces
        // To support multi-word categories, users would need to not use spaces or we'd need quoted strings
        var result = _parser.Parse("category:API Services");
        
        Assert.That(result.Category, Is.EqualTo("API"));
        Assert.That(result.GeneralSearchTerms, Has.Count.EqualTo(1));
        Assert.That(result.GeneralSearchTerms[0], Is.EqualTo("Services"));
    }
    
    [Test]
    public void Parse_PartialBoolTrue_ParsesCorrectly()
    {
        var testCases = new[] { "advanced:t", "advanced:tr", "advanced:tru", "advanced:true" };
        
        foreach (var testCase in testCases)
        {
            var result = _parser.Parse(testCase);
            Assert.That(result.Advanced, Is.True, $"Failed for: {testCase}");
        }
    }
    
    [Test]
    public void Parse_PartialBoolFalse_ParsesCorrectly()
    {
        var testCases = new[] { "modified:f", "modified:fa", "modified:fal", "modified:fals", "modified:false" };
        
        foreach (var testCase in testCases)
        {
            var result = _parser.Parse(testCase);
            Assert.That(result.Modified, Is.False, $"Failed for: {testCase}");
        }
    }
    
    [Test]
    public void Parse_PartialBoolCaseInsensitive_ParsesCorrectly()
    {
        var result1 = _parser.Parse("secret:T");
        Assert.That(result1.Secret, Is.True);
        
        var result2 = _parser.Parse("valid:F");
        Assert.That(result2.Valid, Is.False);
        
        var result3 = _parser.Parse("advanced:TR");
        Assert.That(result3.Advanced, Is.True);
    }
    
    [Test]
    public void Parse_AmbiguousPartialBool_MatchesFirstPrefix()
    {
        // "t" matches "true" (comes first in logic)
        var result = _parser.Parse("advanced:t");
        Assert.That(result.Advanced, Is.True);
    }
    
    [Test]
    public void Parse_ComplexFilterWithPartialBools_ParsesCorrectly()
    {
        var result = _parser.Parse("modified:t valid:f database");
        
        Assert.That(result.Modified, Is.True);
        Assert.That(result.Valid, Is.False);
        Assert.That(result.GeneralSearchTerms, Has.Count.EqualTo(1));
        Assert.That(result.GeneralSearchTerms[0], Is.EqualTo("database"));
    }

    [Test]
    public void Parse_Classification_SetsClassification()
    {
        var result = _parser.Parse("classification:Internal");

        Assert.That(result.Classification, Is.EqualTo("Internal"));
        Assert.That(result.IsEmpty, Is.False);
    }

    [Test]
    public void Parse_Classification_CaseInsensitive_PreservesValue()
    {
        var result = _parser.Parse("CLASSIFICATION:internal");
        
        Assert.That(result.Classification, Is.EqualTo("internal"));
    }
}
