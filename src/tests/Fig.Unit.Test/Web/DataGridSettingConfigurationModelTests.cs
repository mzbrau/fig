using System.Collections.Generic;
using System.Linq;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Scripting;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class DataGridSettingConfigurationModelTests
{
    private SettingDefinitionDataContract _settingDefinition = null!;
    private SettingClientConfigurationModel _parent = null!;

    [SetUp]
    public void SetUp()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string)),
            new("Age", typeof(int)),
            new("IsActive", typeof(bool))
        };

        _settingDefinition = new SettingDefinitionDataContract(
            "TestDataGrid",
            "A test data grid",
            null,
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

        var fakeScriptRunner = new Mock<IScriptRunner>();
        _parent = new SettingClientConfigurationModel("TestClient", "Test Description", null, false, fakeScriptRunner.Object);
    }

    [Test]
    public void EvaluateDirty_InitialState_ShouldNotBeDirty()
    {
        // Arrange
        var model = new DataGridSettingConfigurationModel(_settingDefinition, _parent, new SettingPresentation(false));

        // Act
        model.EvaluateDirty();

        // Assert
        Assert.That(model.IsDirty, Is.False, "Initial state should not be dirty");
    }

    [Test]
    public void EvaluateDirty_WhenAddingRow_ShouldBeDirty()
    {
        // Arrange
        var model = new DataGridSettingConfigurationModel(_settingDefinition, _parent, new SettingPresentation(false));
        var newRow = new Dictionary<string, IDataGridValueModel>
        {
            ["Name"] = new DataGridValueModel<string>("John", false, model),
            ["Age"] = new DataGridValueModel<int>(25, false, model),
            ["IsActive"] = new DataGridValueModel<bool>(true, false, model)
        };

        // Act
        model.Value!.Add(newRow);
        model.EvaluateDirty();

        // Assert
        Assert.That(model.IsDirty, Is.True, "Should be dirty after adding a row");
    }

    [Test]
    public void EvaluateDirty_WhenRemovingRow_ShouldBeDirty()
    {
        // Arrange
        var model = new DataGridSettingConfigurationModel(_settingDefinition, _parent, new SettingPresentation(false));
        var row = new Dictionary<string, IDataGridValueModel>
        {
            ["Name"] = new DataGridValueModel<string>("John", false, model),
            ["Age"] = new DataGridValueModel<int>(25, false, model),
            ["IsActive"] = new DataGridValueModel<bool>(true, false, model)
        };
        model.Value!.Add(row);
        model.MarkAsSaved(); // Save current state

        // Act
        model.Value!.Clear();
        model.EvaluateDirty();

        // Assert
        Assert.That(model.IsDirty, Is.True, "Should be dirty after removing a row");
    }

    [Test]
    public void EvaluateDirty_WhenModifyingCellValue_ShouldBeDirty()
    {
        // Arrange
        var model = new DataGridSettingConfigurationModel(_settingDefinition, _parent, new SettingPresentation(false));
        var row = new Dictionary<string, IDataGridValueModel>
        {
            ["Name"] = new DataGridValueModel<string>("John", false, model),
            ["Age"] = new DataGridValueModel<int>(25, false, model),
            ["IsActive"] = new DataGridValueModel<bool>(true, false, model)
        };
        model.Value!.Add(row);
        model.MarkAsSaved(); // Save current state

        // Act - Modify a cell value
        row["Name"] = new DataGridValueModel<string>("Jane", false, model);
        model.EvaluateDirty();

        // Assert
        Assert.That(model.IsDirty, Is.True, "Should be dirty after modifying a cell value");
    }

    [Test]
    public void EvaluateDirty_AfterMarkAsSaved_ShouldNotBeDirty()
    {
        // Arrange
        var model = new DataGridSettingConfigurationModel(_settingDefinition, _parent, new SettingPresentation(false));
        var row = new Dictionary<string, IDataGridValueModel>
        {
            ["Name"] = new DataGridValueModel<string>("John", false, model),
            ["Age"] = new DataGridValueModel<int>(25, false, model),
            ["IsActive"] = new DataGridValueModel<bool>(true, false, model)
        };
        model.Value!.Add(row);

        // Act
        model.MarkAsSaved();
        model.EvaluateDirty();

        // Assert
        Assert.That(model.IsDirty, Is.False, "Should not be dirty after saving changes");
    }

    [Test]
    public void GetChangeDiff_WhenFirstRowChanged_ShouldNotShowSecondRowAsRemoved()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var currentData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Johnny", ["Age"] = 26, ["IsActive"] = true }, // Changed first row
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }    // Same second row
        };

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        Assert.That(diff, Contains.Substring("-  [John],[25],[True]"), "Should show original first row as removed");
        Assert.That(diff, Contains.Substring("+ [Johnny],[26],[True]"), "Should show modified first row as added");
        Assert.That(diff, Does.Not.Contain("-  [Jane],[30],[False]"), "Should NOT show second row as removed");
        Assert.That(diff, Does.Not.Contain("+ [Jane],[30],[False]"), "Should NOT show second row as added");
    }

    [Test]
    public void GetChangeDiff_WhenSecondRowChanged_ShouldNotShowFirstRowAsRemoved()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var currentData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },   // Same first row
            new() { ["Name"] = "Janet", ["Age"] = 31, ["IsActive"] = true }   // Changed second row
        };

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        Assert.That(diff, Contains.Substring("-  [Jane],[30],[False]"), "Should show original second row as removed");
        Assert.That(diff, Contains.Substring("+ [Janet],[31],[True]"), "Should show modified second row as added");
        Assert.That(diff, Does.Not.Contain("-  [John],[25],[True]"), "Should NOT show first row as removed");
        Assert.That(diff, Does.Not.Contain("+ [John],[25],[True]"), "Should NOT show first row as added");
    }

    [Test]
    public void GetChangeDiff_WhenRowAddedAtBeginning_ShouldShowCorrectDiff()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var currentData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Bob", ["Age"] = 20, ["IsActive"] = true },    // New first row
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },   // Original first row moved down
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }   // Original second row moved down
        };

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        Assert.That(diff, Contains.Substring("+ [Bob],[20],[True]"), "Should show new first row as added");
        Assert.That(diff, Does.Not.Contain("-  [John],[25],[True]"), "Should NOT show original first row as removed");
        Assert.That(diff, Does.Not.Contain("-  [Jane],[30],[False]"), "Should NOT show original second row as removed");
    }

    [Test]
    public void GetChangeDiff_WhenRowRemovedFromMiddle_ShouldShowCorrectDiff()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false },
            new() { ["Name"] = "Bob", ["Age"] = 35, ["IsActive"] = true }
        };

        var currentData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },   // Same first row
            new() { ["Name"] = "Bob", ["Age"] = 35, ["IsActive"] = true }     // Third row moved up (middle row removed)
        };

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        Assert.That(diff, Contains.Substring("-  [Jane],[30],[False]"), "Should show removed middle row");
        Assert.That(diff, Does.Not.Contain("-  [John],[25],[True]"), "Should NOT show first row as removed");
        Assert.That(diff, Does.Not.Contain("-  [Bob],[35],[True]"), "Should NOT show third row as removed");
        Assert.That(diff, Does.Not.Contain("+ [John],[25],[True]"), "Should NOT show first row as added");
        Assert.That(diff, Does.Not.Contain("+ [Bob],[35],[True]"), "Should NOT show third row as added");
    }

    [Test]
    public void GetChangeDiff_WhenRowsReordered_ShouldShowCorrectDiff()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var currentData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false },  // Second row moved to first
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true }    // First row moved to second
        };

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        // When rows are reordered, it should show the changes at each position
        Assert.That(diff, Contains.Substring("-  [John],[25],[True]"), "Should show original first row as removed from first position");
        Assert.That(diff, Contains.Substring("+ [Jane],[30],[False]"), "Should show Jane as added to first position");
        Assert.That(diff, Contains.Substring("-  [Jane],[30],[False]"), "Should show original second row as removed from second position");
        Assert.That(diff, Contains.Substring("+ [John],[25],[True]"), "Should show John as added to second position");
    }

    [Test]
    public void GetChangeDiff_WhenNoChanges_ShouldReturnEmptyDiff()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var currentData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        Assert.That(diff.Trim(), Is.Empty, "Should return empty diff when no changes");
    }

    [Test]
    public void GetChangeDiff_WhenEntireDataGridReplaced_ShouldShowAllChanges()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var currentData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Alice", ["Age"] = 28, ["IsActive"] = true },
            new() { ["Name"] = "Bob", ["Age"] = 32, ["IsActive"] = false }
        };

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        Assert.That(diff, Contains.Substring("-  [John],[25],[True]"), "Should show original first row as removed");
        Assert.That(diff, Contains.Substring("-  [Jane],[30],[False]"), "Should show original second row as removed");
        Assert.That(diff, Contains.Substring("+ [Alice],[28],[True]"), "Should show new first row as added");
        Assert.That(diff, Contains.Substring("+ [Bob],[32],[False]"), "Should show new second row as added");
    }

    [Test]
    public void GetChangeDiff_WhenEmptyToPopulated_ShouldShowAllAsAdded()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>();

        var currentData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        Assert.That(diff, Contains.Substring("+ [John],[25],[True]"), "Should show first row as added");
        Assert.That(diff, Contains.Substring("+ [Jane],[30],[False]"), "Should show second row as added");
        Assert.That(diff, Does.Not.Contain("-"), "Should not show any removals");
    }

    [Test]
    public void GetChangeDiff_WhenPopulatedToEmpty_ShouldShowAllAsRemoved()
    {
        // Arrange
        var originalData = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "John", ["Age"] = 25, ["IsActive"] = true },
            new() { ["Name"] = "Jane", ["Age"] = 30, ["IsActive"] = false }
        };

        var currentData = new List<Dictionary<string, object?>>();

        var model = CreateDataGridModel(originalData, currentData);

        // Act
        var diff = model.GetChangeDiff();

        // Assert
        Assert.That(diff, Contains.Substring("-  [John],[25],[True]"), "Should show first row as removed");
        Assert.That(diff, Contains.Substring("-  [Jane],[30],[False]"), "Should show second row as removed");
        Assert.That(diff, Does.Not.Contain("+"), "Should not show any additions");
    }

    private DataGridSettingConfigurationModel CreateDataGridModel(
        List<Dictionary<string, object?>> originalData,
        List<Dictionary<string, object?>> currentData)
    {
        var model = new DataGridSettingConfigurationModel(_settingDefinition, _parent, new SettingPresentation(false));
        
        // First, set up the original data and mark as saved to establish the baseline
        var originalValueModels = ConvertToValueModels(originalData, model);
        model.Value = originalValueModels;
        model.MarkAsSaved(); // This will set OriginalValue correctly
        
        // Now set the current data to compare against
        var currentValueModels = ConvertToValueModels(currentData, model);
        model.Value = currentValueModels;
        
        return model;
    }

    private List<Dictionary<string, IDataGridValueModel>> ConvertToValueModels(
        List<Dictionary<string, object?>> data,
        DataGridSettingConfigurationModel parent)
    {
        var result = new List<Dictionary<string, IDataGridValueModel>>();
        
        foreach (var row in data)
        {
            var valueModelRow = new Dictionary<string, IDataGridValueModel>();
            foreach (var kvp in row)
            {
                var column = _settingDefinition.DataGridDefinition?.Columns.FirstOrDefault(c => c.Name == kvp.Key);
                if (column != null)
                {
                    var valueModel = new DataGridValueModel<object?>(kvp.Value, false, parent);
                    valueModelRow[kvp.Key] = valueModel;
                }
            }
            result.Add(valueModelRow);
        }
        
        return result;
    }
}
