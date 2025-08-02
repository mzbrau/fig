using System.Collections.Generic;
using System.Linq;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ValidValuesHandlerTests
{
    private Mock<ILookupTablesRepository> _mockLookupTablesRepository = null!;
    private Mock<ILogger<ValidValuesHandler>> _mockLogger = null!;
    private ValidValuesHandler _validValuesHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLookupTablesRepository = new Mock<ILookupTablesRepository>();
        _mockLogger = new Mock<ILogger<ValidValuesHandler>>();
        _validValuesHandler = new ValidValuesHandler(_mockLookupTablesRepository.Object, _mockLogger.Object);
    }

    [Test]
    public void GetValueFromValidValues_WithDisplayValueAndLookupKeySetting_ShouldReturnDisplayValue()
    {
        // Arrange
        var displayValue = "Item1";
        var validValues = new List<string> { "[Bug]Item1", "[Bug]Item2", "[Incident]Item3" };
        var lookupKeySettingName = "IssueType";

        // Act
        var result = _validValuesHandler.GetValueFromValidValues(displayValue, validValues, null, lookupKeySettingName);

        // Assert
        Assert.That(result, Is.TypeOf<StringSettingBusinessEntity>());
        var stringResult = (StringSettingBusinessEntity)result;
        Assert.That(stringResult.Value, Is.EqualTo("Item1"));
    }

    [Test]
    public void GetValueFromValidValues_WithDisplayValueButNoLookupKeySetting_ShouldReturnFirstValue()
    {
        // Arrange
        var displayValue = "Item1";
        var validValues = new List<string> { "[Bug]Item1", "[Bug]Item2", "[Incident]Item3" };

        // Act
        var result = _validValuesHandler.GetValueFromValidValues(displayValue, validValues, null);

        // Assert
        Assert.That(result, Is.TypeOf<StringSettingBusinessEntity>());
        var stringResult = (StringSettingBusinessEntity)result;
        Assert.That(stringResult.Value, Is.EqualTo("[Bug]Item1")); // Should return first value as fallback
    }

    [Test]
    public void GetValueFromValidValues_WithExactMatch_ShouldReturnExactMatch()
    {
        // Arrange
        var value = "[Bug]Item1";
        var validValues = new List<string> { "[Bug]Item1", "[Bug]Item2", "[Incident]Item3" };
        var lookupKeySettingName = "IssueType";

        // Act
        var result = _validValuesHandler.GetValueFromValidValues(value, validValues, null, lookupKeySettingName);

        // Assert
        Assert.That(result, Is.TypeOf<StringSettingBusinessEntity>());
        var stringResult = (StringSettingBusinessEntity)result;
        Assert.That(stringResult.Value, Is.EqualTo("[Bug]Item1"));
    }

    [Test]
    public void GetValueFromValidValues_WithInvalidDisplayValue_ShouldReturnFirstValue()
    {
        // Arrange
        var displayValue = "NonExistentItem";
        var validValues = new List<string> { "[Bug]Item1", "[Bug]Item2", "[Incident]Item3" };
        var lookupKeySettingName = "IssueType";

        // Act
        var result = _validValuesHandler.GetValueFromValidValues(displayValue, validValues, null, lookupKeySettingName);

        // Assert
        Assert.That(result, Is.TypeOf<StringSettingBusinessEntity>());
        var stringResult = (StringSettingBusinessEntity)result;
        Assert.That(stringResult.Value, Is.EqualTo("[Bug]Item1")); // Should return first value as fallback
    }

    [Test]
    public void GetValidValues_WithDisplayValueInLookupTable_ShouldNotMarkAsInvalid()
    {
        // Arrange
        var mockLookupTable = new LookupTableBusinessEntity
        {
            Name = "TestTable",
            LookupTable = new Dictionary<string, string?>
            {
                { "[Bug]In Progress", "Bug In Progress" },
                { "[Bug]Done", "Bug Done" },
                { "[Feature]In Progress", "Feature In Progress" }
            }
        };

        _mockLookupTablesRepository.Setup(x => x.GetAllItems())
            .ReturnsAsync(new List<LookupTableBusinessEntity> { mockLookupTable });

        var currentValue = new StringSettingBusinessEntity("In Progress");

        // Act
        var result = _validValuesHandler.GetValidValues(null, "TestTable", typeof(string), currentValue).Result;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Any(v => v.Contains("[INVALID]")), Is.False, 
            "Should not mark 'In Progress' as invalid when it exists as a suffix in the lookup table");
    }
}
