using System.Collections.Generic;
using System.Linq;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Contracts.SettingDefinitions;
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

    [Test]
    public void GetValue_WithDataGridIntColumn_ParsesValueBeforeSeparator()
    {
        var items = new List<Dictionary<string, object?>>
        {
            new() { ["Count"] = "42 -> forty-two" },
            new() { ["Count"] = "7" }
        };
        var value = new DataGridSettingBusinessEntity(items);
        var definition = new DataGridDefinitionDataContract(
            [new DataGridColumnDataContract("Count", typeof(int))],
            isLocked: false);

        var result = _validValuesHandler.GetValue(
            value,
            ["42 -> forty-two", "7"],
            typeof(List<Dictionary<string, object>>),
            null,
            definition);

        Assert.That(result, Is.SameAs(value));
        Assert.That(items[0]["Count"], Is.EqualTo(42));
        Assert.That(items[1]["Count"], Is.EqualTo(7));
    }

    [Test]
    public void GetValue_WithDataGridBoolColumn_ParsesBooleanStrings()
    {
        var items = new List<Dictionary<string, object?>>
        {
            new() { ["Enabled"] = "true -> yes" },
            new() { ["Enabled"] = "false" }
        };
        var value = new DataGridSettingBusinessEntity(items);
        var definition = new DataGridDefinitionDataContract(
            [new DataGridColumnDataContract("Enabled", typeof(bool))],
            isLocked: false);

        _validValuesHandler.GetValue(
            value,
            ["true -> yes", "false"],
            typeof(List<Dictionary<string, object>>),
            null,
            definition);

        Assert.That(items[0]["Enabled"], Is.EqualTo(true));
        Assert.That(items[1]["Enabled"], Is.EqualTo(false));
    }

    [Test]
    public void GetValue_WithDataGridUnparseableValue_LeavesOriginal()
    {
        var items = new List<Dictionary<string, object?>>
        {
            new() { ["Count"] = "not-a-number -> bad" }
        };
        var value = new DataGridSettingBusinessEntity(items);
        var definition = new DataGridDefinitionDataContract(
            [new DataGridColumnDataContract("Count", typeof(int))],
            isLocked: false);

        _validValuesHandler.GetValue(
            value,
            ["not-a-number -> bad"],
            typeof(List<Dictionary<string, object>>),
            null,
            definition);

        Assert.That(items[0]["Count"], Is.EqualTo("not-a-number -> bad"));
    }

    [Test]
    public void GetValueFromValidValues_WithDataGridRows_MapsFirstColumnDisplayValues()
    {
        var rows = new List<Dictionary<string, object>>
        {
            new() { ["Values"] = "Item1" },
            new() { ["Values"] = "[Bug]Item2" }
        };
        var validValues = new List<string> { "[Bug]Item1", "[Bug]Item2", "[Incident]Item3" };

        var result = _validValuesHandler.GetValueFromValidValues(rows, validValues, null, "IssueType");

        Assert.That(result, Is.TypeOf<DataGridSettingBusinessEntity>());
        var grid = (DataGridSettingBusinessEntity)result;
        var list = (List<Dictionary<string, object?>>)grid.GetValue()!;
        Assert.That(list[0]["Values"], Is.EqualTo("Item1"));
        Assert.That(list[1]["Values"], Is.EqualTo("[Bug]Item2"));
    }

    [Test]
    public void GetValue_WithScalarSeparatorValue_ParsesBeforeArrow()
    {
        var value = new StringSettingBusinessEntity("99 -> ninety-nine");

        var result = _validValuesHandler.GetValue(value, ["99 -> ninety-nine"], typeof(int), null, null);

        Assert.That(result, Is.TypeOf<IntSettingBusinessEntity>());
        Assert.That(((IntSettingBusinessEntity)result!).Value, Is.EqualTo(99));
    }
}
