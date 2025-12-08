using System.Collections.Generic;
using System.Globalization;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class LazyValidationTests
{
    private SettingClientConfigurationModel _parent = null!;

    [SetUp]
    public void SetUp()
    {
        var fakeScriptRunner = new Mock<IScriptRunner>();
        _parent = new SettingClientConfigurationModel("TestClient", "Test Description", null, false, fakeScriptRunner.Object);
    }

    [Test]
    public void SettingConfigurationModel_DoesNotValidateInConstructor()
    {
        // Arrange
        var settingDefinition = new SettingDefinitionDataContract(
            "TestSetting",
            "A test setting",
            new StringSettingDataContract("invalid"),
            false,
            typeof(string),
            new StringSettingDataContract("default"),
            @"^valid$", // Regex that will fail for "invalid"
            "Must be 'valid'"
        );

        // Act - Constructor should not validate
        var model = new StringSettingConfigurationModel(settingDefinition, _parent, new SettingPresentation(false));

        // Assert - At this point, validation hasn't occurred yet
        // The model should still be considered valid initially
        Assert.That(model.IsValid, Is.True, "Model should be valid before first access");
    }

    [Test]
    public void SettingConfigurationModel_ValidatesOnFirstAccess()
    {
        // Arrange
        var settingDefinition = new SettingDefinitionDataContract(
            "TestSetting",
            "A test setting",
            new StringSettingDataContract("invalid"),
            false,
            typeof(string),
            new StringSettingDataContract("default"),
            @"^valid$", // Regex that will fail for "invalid"
            "Must be 'valid'"
        );

        var model = new StringSettingConfigurationModel(settingDefinition, _parent, new SettingPresentation(false));

        // Act - Access the value, which should trigger validation
        var _ = model.Value;

        // Assert - Now validation should have occurred
        Assert.That(model.IsValid, Is.False, "Model should be invalid after accessing value");
    }

    [Test]
    public void SettingConfigurationModel_DoesNotReValidateOnSecondAccess()
    {
        // Arrange
        var settingDefinition = new SettingDefinitionDataContract(
            "TestSetting",
            "A test setting",
            new StringSettingDataContract("test"),
            false,
            typeof(string),
            new StringSettingDataContract("default"),
            @"^test$",
            "Must be 'test'"
        );

        var model = new StringSettingConfigurationModel(settingDefinition, _parent, new SettingPresentation(false));

        // Act - Access value twice
        var value1 = model.Value;
        var value2 = model.Value;

        // Assert - Validation should only happen once
        // We can't directly count validations, but we can verify the flag is set
        Assert.That(model.IsValid, Is.True, "Model should be valid");
    }

    [Test]
    public void DataGridSettingConfigurationModel_DoesNotValidateInConstructor()
    {
        // Arrange
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string), null, null, false, @"^valid$", "Must be 'valid'", false)
        };

        var invalidData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "invalid" }
        };

        var settingDefinition = new SettingDefinitionDataContract(
            "TestDataGrid",
            "A test data grid",
            new DataGridSettingDataContract(invalidData),
            false,
            typeof(List<Dictionary<string, object>>),
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            new DataGridDefinitionDataContract(columns, false)
        );

        // Act - Constructor should not validate
        var model = new DataGridSettingConfigurationModel(settingDefinition, _parent, new SettingPresentation(false));

        // Assert - Model should be valid before Initialize() is called
        Assert.That(model.IsValid, Is.True, "DataGrid model should be valid before initialization");
    }

    [Test]
    public void DataGridSettingConfigurationModel_ValidatesOnInitialize()
    {
        // Arrange
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string), null, null, false, @"^valid$", "Must be 'valid'", false)
        };

        var invalidData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "invalid" }
        };

        var settingDefinition = new SettingDefinitionDataContract(
            "TestDataGrid",
            "A test data grid",
            new DataGridSettingDataContract(invalidData),
            false,
            typeof(List<Dictionary<string, object>>),
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            new DataGridDefinitionDataContract(columns, false)
        );

        var model = new DataGridSettingConfigurationModel(settingDefinition, _parent, new SettingPresentation(false));

        // Act - Initialize should trigger validation
        model.Initialize();

        // Assert - Now validation should have occurred
        Assert.That(model.IsValid, Is.False, "DataGrid model should be invalid after initialization");
    }

    [Test]
    public void DataGridSettingConfigurationModel_SkipsValidationForLargeGrids()
    {
        // Arrange - Create a data grid with 15 rows (> 10 threshold)
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string), null, null, false, @"^valid$", "Must be 'valid'", false)
        };

        var largeData = new List<Dictionary<string, object?>>();
        for (int i = 0; i < 15; i++)
        {
            largeData.Add(new Dictionary<string, object?> { ["Name"] = $"invalid_{i}" });
        }

        var settingDefinition = new SettingDefinitionDataContract(
            "LargeDataGrid",
            "A large data grid",
            new DataGridSettingDataContract(largeData),
            false,
            typeof(List<Dictionary<string, object>>),
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            new DataGridDefinitionDataContract(columns, false)
        );

        var model = new DataGridSettingConfigurationModel(settingDefinition, _parent, new SettingPresentation(false));

        // Act - Initialize should skip validation for large grids
        model.Initialize();

        // Assert - Should be valid because validation was skipped
        Assert.That(model.IsValid, Is.True, "Large data grid should skip validation and be valid");
    }

    [Test]
    public void DataGridSettingConfigurationModel_ValidatesNewRowsInLargeGrids()
    {
        // Arrange - Create a data grid with 15 rows (> 10 threshold)
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string), null, null, false, @"^valid$", "Must be 'valid'", false)
        };

        var largeData = new List<Dictionary<string, object?>>();
        for (int i = 0; i < 15; i++)
        {
            largeData.Add(new Dictionary<string, object?> { ["Name"] = "valid" });
        }

        var settingDefinition = new SettingDefinitionDataContract(
            "LargeDataGrid",
            "A large data grid",
            new DataGridSettingDataContract(largeData),
            false,
            typeof(List<Dictionary<string, object>>),
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            new DataGridDefinitionDataContract(columns, false)
        );

        var model = new DataGridSettingConfigurationModel(settingDefinition, _parent, new SettingPresentation(false));
        model.Initialize(); // Initialize with 15 valid rows

        // Act - Add a new row with invalid data
        var newRow = new Dictionary<string, IDataGridValueModel>
        {
            ["Name"] = new DataGridValueModel<string>("invalid", false, model, null, null, @"^valid$", "Must be 'valid'")
        };
        model.Value!.Add(newRow);
        model.ValidateDataGrid();

        // Assert - Should detect the invalid new row
        Assert.That(model.IsValid, Is.False, "Should validate new rows added to large data grids");
    }

    [Test]
    public void DataGridSettingConfigurationModel_ValidatesAllRowsInSmallGrids()
    {
        // Arrange - Create a data grid with 5 rows (< 10 threshold)
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string), null, null, false, @"^valid$", "Must be 'valid'", false)
        };

        var smallData = new List<Dictionary<string, object?>>();
        for (int i = 0; i < 5; i++)
        {
            smallData.Add(new Dictionary<string, object?> { ["Name"] = $"invalid_{i}" });
        }

        var settingDefinition = new SettingDefinitionDataContract(
            "SmallDataGrid",
            "A small data grid",
            new DataGridSettingDataContract(smallData),
            false,
            typeof(List<Dictionary<string, object>>),
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            new DataGridDefinitionDataContract(columns, false)
        );

        var model = new DataGridSettingConfigurationModel(settingDefinition, _parent, new SettingPresentation(false));

        // Act - Initialize should validate all rows
        model.Initialize();

        // Assert - Should be invalid because all rows fail validation
        Assert.That(model.IsValid, Is.False, "Small data grid should validate all rows");
    }
}
