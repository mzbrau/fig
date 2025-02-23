using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Common.Events;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Scripting;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class ScriptRunnerTests
{
    private IScriptRunner _runner = default!;
    private SettingClientConfigurationModel _model = default!;
    
    [SetUp]
    public void Setup()
    {
        _runner = CreateRunner();
        _model = CreateModel();
    }
    
    [Test]
    public void ShallExecuteValidJavascript()
    {
        _runner.RunScript("if (one.Value == 'oneValue') { two.Value = 'cat'; } else { two.Value = 'dog'; }", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "two").GetStringValue(), Is.EqualTo("cat"));
    }

    [Test]
    public void ShallHandleInvalidJavascript()
    {
        Assert.DoesNotThrow(() => _runner.RunScript("some invalid javascript", _model));
    }

    [Test]
    public void ShallReturnOriginalIfBeautifyScriptIsNotAvailable()
    {
        var script = "if (one.Value == 'oneValue') { two.Value = 'cat'; } else { two.Value = 'dog'; }";
        var result = _runner.FormatScript(script);
        
        Assert.That(result, Is.EqualTo(script));
    }

    [Test]
    public void ShallLoadAllSettingsIntoContext()
    {
        _runner.RunScript("one.Value = 'oneX'; two.Value = 'twoX'; three.Value = 'threeX';", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").GetStringValue(), Is.EqualTo("oneX"));
        Assert.That(_model.Settings.Single(a => a.Name == "two").GetStringValue(), Is.EqualTo("twoX"));
        Assert.That(_model.Settings.Single(a => a.Name == "three").GetStringValue(), Is.EqualTo("threeX"));
    }

    [Test]
    public void ShallNotUpdateSettingName()
    {
        _runner.RunScript("one.Name = 'aNewName';", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").Name, Is.EqualTo("one"));
    }

    [Test]
    public void ShallUpdateSettingIsValid()
    {
        _runner.RunScript("one.IsValid = false;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").IsValid, Is.EqualTo(false));
        
        _runner.RunScript("one.IsValid = true;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").IsValid, Is.EqualTo(true));
    }

    [Test]
    public void ShallUpdateSettingIsValidExplanation()
    {
        _runner.RunScript("one.ValidationExplanation = 'some explanation';", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").ValidationExplanation, Is.EqualTo("some explanation"));
    }

    [Test]
    public void ShallUpdateSettingAdvanced()
    {
        _runner.RunScript("one.Advanced = true;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").Advanced, Is.EqualTo(true));
        
        _runner.RunScript("one.Advanced = false;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").Advanced, Is.EqualTo(false));
    }

    [Test]
    public void ShallUpdateSettingDisplayOrder()
    {
        _runner.RunScript("one.DisplayOrder = 4;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").DisplayOrder, Is.EqualTo(4));
    }

    [Test]
    public void ShallUpdateSettingVisible()
    {
        _runner.RunScript("one.IsVisible = true;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").Hidden, Is.EqualTo(false));
        
        _runner.RunScript("one.IsVisible = false;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").Hidden, Is.EqualTo(true));
    }

    [Test]
    public void ShallUpdateSettingCategoryColor()
    {
        _runner.RunScript("one.CategoryColor = '#cc4e58';", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").CategoryColor, Is.EqualTo("#cc4e58"));
    }

    [Test]
    public void ShallUpdateSettingCategoryName()
    {
        _runner.RunScript("one.CategoryName = 'thing';", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").CategoryName, Is.EqualTo("thing"));
    }

    [Test]
    public void ShallUpdateSettingReadOnly()
    {
        _runner.RunScript("one.IsReadOnly = true;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").IsReadOnly, Is.EqualTo(true));
    }

    [Test]
    public void ShallUpdateSettingStringValue()
    {
        _runner.RunScript("one.Value = 'some new value';", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").GetStringValue(), Is.EqualTo("some new value"));
    }
    
    [Test]
    public void ShallUpdateSettingIntValue()
    {
        _runner.RunScript("five.Value = 5;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "five").GetValue(true), Is.EqualTo(5));
    }
    
    [Test]
    public void ShallUpdateSettingLongValue()
    {
        _runner.RunScript("six.Value = 7;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "six").GetValue(true), Is.EqualTo(7L));
    }
    
    [Test]
    public void ShallUpdateSettingBoolValue()
    {
        _runner.RunScript("seven.Value = true;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "seven").GetValue(true), Is.EqualTo(true));
    }
    
    [Test]
    public void ShallUpdateSettingDoubleValue()
    {
        _runner.RunScript("eight.Value = 3.3;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "eight").GetValue(true), Is.EqualTo(3.3));
    }
    
    [Test]
    public void ShallUpdateSettingDateTimeValue()
    {
        _runner.RunScript("nine.Value = new Date(2023, 11, 23, 12, 30, 0, 0);", _model);

        var dateTime = _model.Settings.Single(a => a.Name == "nine").GetValue(true) as DateTime?;
        Assert.That(dateTime!.Value.Year, Is.EqualTo(2023));
    }
    
    [Test]
    public void ShallUpdateSettingTimeSpanValue()
    {
        _runner.RunScript("ten.Value = 5000", _model);
        
        var timespan = _model.Settings.Single(a => a.Name == "ten").GetValue(true) as TimeSpan?;
        Assert.That(timespan!.Value.TotalMilliseconds, Is.EqualTo(5000));
    }

    [Test]
    public void ShallUpdateDataGridValue()
    {
        _runner.RunScript("eleven.Value[0].Name = 'todd'; eleven.Value[1].Name = 'rachel'; eleven.Value[0].Age = 90", _model);
        
        var dataGrid =  _model.Settings.Single(a => a.Name == "eleven").GetValue(true) as List<Dictionary<string, IDataGridValueModel>>;
        Assert.That(dataGrid![0]["Name"].ReadOnlyValue, Is.EqualTo("todd"));
        Assert.That(dataGrid[1]["Name"].ReadOnlyValue, Is.EqualTo("rachel"));
        Assert.That(dataGrid[0]["Age"].ReadOnlyValue, Is.EqualTo(90));
    }

    [Test]
    public void ShallUpdateDataGridValidValues()
    {
        _runner.RunScript("eleven.ValidValues[0].Pet = [\"Spider\", \"Snake\", \"Parrot\", \"Horse\"];", _model);
        
        var dataGrid =  _model.Settings.Single(a => a.Name == "eleven").GetValue(true) as List<Dictionary<string, IDataGridValueModel>>;
        Assert.That(string.Join(",", dataGrid![0]["Pet"].ValidValues!), Is.EqualTo("Spider,Snake,Parrot,Horse"));
        Assert.That(string.Join(",", dataGrid[1]["Pet"].ValidValues!), Is.EqualTo("cat,dog,rabbit"), "Existing values should not have been changed");
    }
    
    [Test]
    public void ShallUpdateAllRowsInDataGridValidValues()
    {
        _runner.RunScript("for (let item of eleven.ValidValues) { item.Pet = [\"Spider\", \"Snake\", \"Parrot\", \"Horse\"]; }", _model);
        
        var dataGrid =  _model.Settings.Single(a => a.Name == "eleven").GetValue(true) as List<Dictionary<string, IDataGridValueModel>>;
        Assert.That(string.Join(",", dataGrid![0]["Pet"].ValidValues!), Is.EqualTo("Spider,Snake,Parrot,Horse"));
        Assert.That(string.Join(",", dataGrid[1]["Pet"].ValidValues!), Is.EqualTo("Spider,Snake,Parrot,Horse"));
    }
    
    [Test]
    public void ShallUpdateValueWithValuesFromOtherSetting()
    {
        _runner.RunScript("one.Value = twelve.Value.map(group => group.Values).join();", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").GetValue(), Is.EqualTo("shark,penguin"));
    }
    
    [Test]
    public void ShallUpdateValidValuesWithValuesFromOtherSetting()
    {
        const string script = @"
var values = twelve.Value.map(group => group.Values);
for (let item of eleven.ValidValues) {
item.Pet = values;
}";
        _runner.RunScript(script, _model);

        var dataGrid =  _model.Settings.Single(a => a.Name == "eleven").GetValue(true) as List<Dictionary<string, IDataGridValueModel>>;
        Assert.That(string.Join(",", dataGrid![0]["Pet"].ValidValues!), Is.EqualTo("shark,penguin"));
        Assert.That(string.Join(",", dataGrid[1]["Pet"].ValidValues!), Is.EqualTo("shark,penguin"));
    }
    
    [Test]
    public void ShallUpdateDataGridValidValuesAndValue()
    {
        _runner.RunScript("eleven.ValidValues[0].Pet = [\"Spider\", \"Snake\"]; eleven.Value[0].Pet = 'Snake'", _model);
        
        var dataGrid =  _model.Settings.Single(a => a.Name == "eleven").GetValue(true) as List<Dictionary<string, IDataGridValueModel>>;
        Assert.That(string.Join(",", dataGrid![0]["Pet"].ValidValues!), Is.EqualTo("Spider,Snake"));
        Assert.That(string.Join(",", dataGrid[0]["Pet"].ReadOnlyValue!), Is.EqualTo("Snake"));
    }
    
    [Test]
    public void ShallUpdateDataGridIsReadOnly()
    {
        _runner.RunScript("eleven.IsReadOnly[0].Pet = true; eleven.IsReadOnly[1].Name = true", _model);
        
        var dataGrid = _model.Settings.Single(a => a.Name == "eleven").GetValue(true) as List<Dictionary<string, IDataGridValueModel>>;
        Assert.That(dataGrid![1]["Name"].IsReadOnly, Is.EqualTo(true));
        Assert.That(dataGrid[0]["Pet"].IsReadOnly, Is.EqualTo(true));
    }

    [Test]
    public void ShallUpdateDataGridValidationErrors()
    {
        _runner.RunScript("eleven.ValidationErrors[0].Name = 'Name should include both first and last name'", _model);
        
        var dataGrid =  _model.Settings.Single(a => a.Name == "eleven");
        Assert.That(dataGrid.IsValid, Is.EqualTo(false));
        Assert.That(dataGrid.ValidationExplanation, Is.EqualTo("[Name - mike] Name should include both first and last name"));
    }
    
    [Test]
    public void ShallNotAddValidationErrorsToDataGrid()
    {
        _runner.RunScript("log(eleven.ValidationErrors.Count)", _model);
        
        var dataGrid =  _model.Settings.Single(a => a.Name == "eleven");
        Assert.That(dataGrid.IsValid, Is.EqualTo(true));
    }
    
    [Test]
    public void ShallUpdateDataGridEditorLineCount()
    {
        _runner.RunScript("eleven.EditorLineCount[0].Name = 3", _model);
        
        var dataGrid =  _model.Settings.Single(a => a.Name == "eleven").GetValue(true) as List<Dictionary<string, IDataGridValueModel>>;
        Assert.That(dataGrid![0]["Name"].EditorLineCount, Is.EqualTo(3));
    }

    [Test]
    public void ShallUpdateEditorLineCount()
    {
        _runner.RunScript("one.EditorLineCount = 4;", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").EditorLineCount, Is.EqualTo(4));
    }

    [Test]
    public void ShallUpdateValidValues()
    {
        _runner.RunScript("four.ValidValues = [\"Apple\", \"Banana\", \"Cherry\", \"Date\"];", _model);

        var setting = _model.Settings.Single(a => a.Name == "four") as DropDownSettingConfigurationModel;
        Assert.That(string.Join(",", setting!.ValidValues), Is.EqualTo("Apple,Banana,Cherry,Date"));
    }
    
    [Test]
    public void ShallResetInfiniteLoopProtectionWithManualValueUpdate()
    {
        _runner.RunScript("one.Value = 'new';", _model);
        _runner.RunScript("one.Value = 'new2';", _model);
        
        _model.Settings.Single(a => a.Name == "one").SetValue("manual");
        
        
        _runner.RunScript("one.Value = 'new3';", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").GetStringValue(), Is.EqualTo("new3"));
    }

    private SettingClientConfigurationModel CreateModel()
    {
        var model = new SettingClientConfigurationModel("test", "test", null, true, Mock.Of<IScriptRunner>());
        model.Settings = new List<ISetting>()
        {
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("one", "", new StringSettingDataContract("oneValue"),
                    valueType: typeof(string)), model, false),
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("two", "", new StringSettingDataContract("twoValue"),
                    valueType: typeof(string)), model, false),
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("three", "", new StringSettingDataContract("threeValue"),
                    valueType: typeof(string)), model, false),
            new DropDownSettingConfigurationModel(
                new SettingDefinitionDataContract("four", "", new StringSettingDataContract("fourValue"),
                    valueType: typeof(string)), model, false),
            new IntSettingConfigurationModel(
                new SettingDefinitionDataContract("five", "", new IntSettingDataContract(1),
                    valueType: typeof(int)), model, false),
            new LongSettingConfigurationModel(
                new SettingDefinitionDataContract("six", "", new LongSettingDataContract(1),
                    valueType: typeof(long)), model, false),
            new BoolSettingConfigurationModel(
                new SettingDefinitionDataContract("seven", "", new BoolSettingDataContract(false),
                    valueType: typeof(bool)), model, false),
            new DoubleSettingConfigurationModel(
                new SettingDefinitionDataContract("eight", "", new DoubleSettingDataContract(1.1),
                    valueType: typeof(double)), model, false),
            new DateTimeSettingConfigurationModel(
                new SettingDefinitionDataContract("nine", "", new DateTimeSettingDataContract(DateTime.Now),
                    valueType: typeof(DateTime)), model, false),
            new TimeSpanSettingConfigurationModel(
                new SettingDefinitionDataContract("ten", "", new TimeSpanSettingDataContract(TimeSpan.FromSeconds(1)),
                    valueType: typeof(TimeSpan)), model, false),
            new DataGridSettingConfigurationModel(
                new SettingDefinitionDataContract("eleven", "", new DataGridSettingDataContract(
                        new List<Dictionary<string, object?>>
                        {
                            new()
                            {
                                { "Name", "mike" },
                                { "Age", 30L },
                                { "Pet", "cat" },
                            },
                            new()
                            {
                                { "Name", "john" },
                                { "Age", 25L },
                                { "Pet", "dog" },
                            }
                        }),
                    valueType: typeof(List<Dictionary<string, object?>>),
                    dataGridDefinition: new DataGridDefinitionDataContract(new List<DataGridColumnDataContract>()
                    {
                        new("Name", typeof(string)),
                        new("Age", typeof(int)),
                        new("Pet", typeof(string), new List<string> { "cat", "dog", "rabbit" }),
                    }, false)), model, false),
            new DataGridSettingConfigurationModel(
                new SettingDefinitionDataContract("twelve", "", new DataGridSettingDataContract(
                        new List<Dictionary<string, object?>>
                        {
                            new()
                            {
                                { "Values", "shark" }
                            },
                            new()
                            {
                                { "Values", "penguin" }
                            }
                        }),
                    valueType: typeof(List<Dictionary<string, object?>>),
                    dataGridDefinition: new DataGridDefinitionDataContract(new List<DataGridColumnDataContract>()
                    {
                        new("Values", typeof(string))
                    }, false)), model, false),
        };

        return model;
    }

    private ScriptRunner CreateRunner()
    {
        return new ScriptRunner(Mock.Of<IBeautifyLoader>(),
            Mock.Of<IEventDistributor>(),
            Mock.Of<IInfiniteLoopDetector>());
    }
}