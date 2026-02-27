using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Scripting;
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
    private IScriptRunner _runner = null!;
    private IScriptableClient _model = null!;
    private readonly IJsEngineFactory _jsEngineFactory = new JintEngineFactory();
    
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
        
        Assert.That(_model.Settings.Single(a => a.Name == "two").GetValue(), Is.EqualTo("cat"));
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
        var runner = new ScriptRunner(Mock.Of<IInfiniteLoopDetector>(), _jsEngineFactory);
        var result = runner.FormatScript(script);
        
        Assert.That(result, Is.EqualTo(script));
    }

    [Test]
    public void ShallLoadAllSettingsIntoContext()
    {
        _runner.RunScript("one.Value = 'oneX'; two.Value = 'twoX'; three.Value = 'threeX';", _model);
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").GetValue(), Is.EqualTo("oneX"));
        Assert.That(_model.Settings.Single(a => a.Name == "two").GetValue(), Is.EqualTo("twoX"));
        Assert.That(_model.Settings.Single(a => a.Name == "three").GetValue(), Is.EqualTo("threeX"));
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
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").GetValue(), Is.EqualTo("some new value"));
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
        
        Assert.That(_model.Settings.Single(a => a.Name == "one").GetValue(), Is.EqualTo("new3"));
    }

    [Test]
    public void ShallAllowAccessToNestedSettingsByDotNotation()
    {
        var model = CreateNestedSettingsModel();
        
        // Test accessing nested setting by dot notation
        _runner.RunScript("MessageBus.Auth.Username.Value = 'John';", model);
        
        Assert.That(model.Settings.Single(a => a.Name == "MessageBus->Auth->Username").GetValue(), Is.EqualTo("John"));
    }
    
    [Test]
    public void ShallAllowAccessToNestedSettingsByFullNameAndDotNotation()
    {
        var model = CreateNestedSettingsModel();
        
        // Test that both full name and dot notation work
        _runner.RunScript("MessageBus.Auth.Username.Value = 'Alice'; MessageBus.Uri.IsVisible = false;", model);
        
        Assert.That(model.Settings.Single(a => a.Name == "MessageBus->Auth->Username").GetValue(), Is.EqualTo("Alice"));
        Assert.That(model.Settings.Single(a => a.Name == "MessageBus->Uri").Hidden, Is.EqualTo(true));
    }
    
    [Test]    
    public void ShallHandleConflictingSettingsWithDotNotation()
    {
        var model = CreateConflictingNestedSettingsModel();
        
        // Should not throw an exception when there are conflicting property names
        Assert.DoesNotThrow(() => _runner.RunScript("log('test');", model));
        
        // Should be able to access by dot notation
        _runner.RunScript("Database1.TimeoutMs.Value = 5000; Database2.TimeoutMs.Value = 10000;", model);
        
        Assert.That(model.Settings.Single(a => a.Name == "Database1->TimeoutMs").GetValue(), Is.EqualTo(5000));
        Assert.That(model.Settings.Single(a => a.Name == "Database2->TimeoutMs").GetValue(), Is.EqualTo(10000));
    }
    
    [Test]
    public void ShallWorkWithDeeplyNestedSettingsUsingDotNotation()
    {
        var model = CreateDeeplyNestedSettingsModel();
        
        // Test accessing deeply nested setting by dot notation
        _runner.RunScript("App.MessageBus.Auth.Password.Value = 'secret123';", model);
        
        Assert.That(model.Settings.Single(a => a.Name == "App->MessageBus->Auth->Password").GetValue(), Is.EqualTo("secret123"));
    }
    
    [Test]
    public void ShallAllowAccessToNestedSettingsByLeafName()
    {
        var model = CreateNestedSettingsModel();
        
        _runner.RunScript("Username.Value = 'Bob';", model);
        
        Assert.That(model.Settings.Single(a => a.Name == "MessageBus->Auth->Username").GetValue(), Is.EqualTo("Bob"));
    }
    
    [Test]
    public void ShallAllowLeafNameAndDotNotationTogether()
    {
        var model = CreateNestedSettingsModel();
        
        _runner.RunScript("Username.Value = 'Eve'; MessageBus.Uri.IsVisible = false;", model);
        
        Assert.That(model.Settings.Single(a => a.Name == "MessageBus->Auth->Username").GetValue(), Is.EqualTo("Eve"));
        Assert.That(model.Settings.Single(a => a.Name == "MessageBus->Uri").Hidden, Is.EqualTo(true));
    }
    
    [Test]
    public void ShallAllowLeafNameForDeeplyNestedSettings()
    {
        var model = CreateDeeplyNestedSettingsModel();
        
        _runner.RunScript("Password.Value = 'newpass';", model);
        
        Assert.That(model.Settings.Single(a => a.Name == "App->MessageBus->Auth->Password").GetValue(), Is.EqualTo("newpass"));
    }

    private IScriptableClient CreateModel()
    {
        var presentation = new SettingPresentation(false);
        var model = new SettingClientConfigurationModel("test", "test", null, true, Mock.Of<IScriptRunner>());
        model.Settings = new List<ISetting>()
        {
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("one", "", new StringSettingDataContract("oneValue"),
                    valueType: typeof(string)), model, presentation),
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("two", "", new StringSettingDataContract("twoValue"),
                    valueType: typeof(string)), model, presentation),
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("three", "", new StringSettingDataContract("threeValue"),
                    valueType: typeof(string)), model, presentation),
            new DropDownSettingConfigurationModel(
                new SettingDefinitionDataContract("four", "", new StringSettingDataContract("fourValue"),
                    valueType: typeof(string)), model, presentation),
            new IntSettingConfigurationModel(
                new SettingDefinitionDataContract("five", "", new IntSettingDataContract(1),
                    valueType: typeof(int)), model, presentation),
            new LongSettingConfigurationModel(
                new SettingDefinitionDataContract("six", "", new LongSettingDataContract(1),
                    valueType: typeof(long)), model, presentation),
            new BoolSettingConfigurationModel(
                new SettingDefinitionDataContract("seven", "", new BoolSettingDataContract(false),
                    valueType: typeof(bool)), model, presentation),
            new DoubleSettingConfigurationModel(
                new SettingDefinitionDataContract("eight", "", new DoubleSettingDataContract(1.1),
                    valueType: typeof(double)), model, presentation),
            new DateTimeSettingConfigurationModel(
                new SettingDefinitionDataContract("nine", "", new DateTimeSettingDataContract(DateTime.Now),
                    valueType: typeof(DateTime)), model, presentation),
            new TimeSpanSettingConfigurationModel(
                new SettingDefinitionDataContract("ten", "", new TimeSpanSettingDataContract(TimeSpan.FromSeconds(1)),
                    valueType: typeof(TimeSpan)), model, presentation),
            new DataGridSettingConfigurationModel(
                new SettingDefinitionDataContract("eleven", "", new DataGridSettingDataContract(
                    [
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
                    ]),
                    valueType: typeof(List<Dictionary<string, object?>>),
                    dataGridDefinition: new DataGridDefinitionDataContract(new List<DataGridColumnDataContract>()
                    {
                        new("Name", typeof(string)),
                        new("Age", typeof(int)),
                        new("Pet", typeof(string), ["cat", "dog", "rabbit"]),
                    }, false)), model, presentation),
            new DataGridSettingConfigurationModel(
                new SettingDefinitionDataContract("twelve", "", new DataGridSettingDataContract(
                    [
                        new()
                        {
                            { "Values", "shark" }
                        },

                        new()
                        {
                            { "Values", "penguin" }
                        }
                    ]),
                    valueType: typeof(List<Dictionary<string, object?>>),
                    dataGridDefinition: new DataGridDefinitionDataContract([
                            new("Values",
                                typeof(string))
                        ],
                        false)),
                model,
                presentation),
        };

        return new ScriptableClientAdapter(model);
    }

    private ScriptRunner CreateRunner()
    {
        return new ScriptRunner(Mock.Of<IInfiniteLoopDetector>(), _jsEngineFactory, Mock.Of<IScriptBeautifier>());
    }
    
    private IScriptableClient CreateNestedSettingsModel()
    {
        var presentation = new SettingPresentation(false);
        var model = new SettingClientConfigurationModel("testNested", "test nested", null, true, Mock.Of<IScriptRunner>());
        model.Settings =
        [
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("MessageBus->Uri", "",
                    new StringSettingDataContract("http://localhost"),
                    valueType: typeof(string)), model, presentation),

            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("MessageBus->Auth->Username", "",
                    new StringSettingDataContract("Frank"),
                    valueType: typeof(string)), model, presentation),

            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("MessageBus->Auth->Password", "",
                    new StringSettingDataContract("secret"),
                    valueType: typeof(string)), model, presentation),

            new IntSettingConfigurationModel(
                new SettingDefinitionDataContract("Database->TimeoutMs", "", new IntSettingDataContract(30000),
                    valueType: typeof(int)), model, presentation)

        ];

        return new ScriptableClientAdapter(model);
    }
    
    private IScriptableClient CreateConflictingNestedSettingsModel()
    {
        var presentation = new SettingPresentation(false);
        var model = new SettingClientConfigurationModel("testConflicting", "test conflicting", null, true, Mock.Of<IScriptRunner>());
        model.Settings = new List<ISetting>()
        {
            new IntSettingConfigurationModel(
                new SettingDefinitionDataContract("Database1->TimeoutMs", "", new IntSettingDataContract(1000),
                    valueType: typeof(int)), model, presentation),
            new IntSettingConfigurationModel(
                new SettingDefinitionDataContract("Database2->TimeoutMs", "", new IntSettingDataContract(2000),
                    valueType: typeof(int)), model, presentation),
        };

        return new ScriptableClientAdapter(model);
    }
    
    private IScriptableClient CreateDeeplyNestedSettingsModel()
    {
        var presentation = new SettingPresentation(false);
        var model = new SettingClientConfigurationModel("testDeep", "test deeply nested", null, true, Mock.Of<IScriptRunner>());
        model.Settings = new List<ISetting>()
        {
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("App->MessageBus->Auth->Username", "", new StringSettingDataContract("admin"),
                    valueType: typeof(string)), model, presentation),
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("App->MessageBus->Auth->Password", "", new StringSettingDataContract(""),
                    valueType: typeof(string)), model, presentation),
            new StringSettingConfigurationModel(
                new SettingDefinitionDataContract("App->Database->ConnectionString", "", new StringSettingDataContract("Server=localhost"),
                    valueType: typeof(string)), model, presentation),
        };

        return new ScriptableClientAdapter(model);
    }
}