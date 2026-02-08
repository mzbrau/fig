using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Common.Events;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Builders;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Moq;
using NUnit.Framework;
using Radzen;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class SettingClientFacadeApplyCompareValueTests
{
    private SettingClientFacade _sut = null!;
    private SettingClientConfigurationModel _client = null!;
    private SettingEventModel? _lastSettingEvent;

    [SetUp]
    public void SetUp()
    {
        var httpService = new Mock<IHttpService>();
        var definitionConverter = new Mock<ISettingsDefinitionConverter>();
        var historyConverter = new Mock<ISettingHistoryConverter>();
        var groupBuilder = new Mock<ISettingGroupBuilder>();
        var notificationService = new NotificationService();
        var notificationFactory = new Mock<INotificationFactory>();
        var clientStatusFacade = new Mock<IClientStatusFacade>();
        var eventDistributor = new Mock<IEventDistributor>();
        var apiVersionFacade = new Mock<IApiVersionFacade>();
        var schedulingFacade = new Mock<ISchedulingFacade>();

        _sut = new SettingClientFacade(
            httpService.Object,
            definitionConverter.Object,
            historyConverter.Object,
            groupBuilder.Object,
            notificationService,
            notificationFactory.Object,
            clientStatusFacade.Object,
            eventDistributor.Object,
            apiVersionFacade.Object,
            schedulingFacade.Object);

        var scriptRunner = Mock.Of<IScriptRunner>();
        _client = new SettingClientConfigurationModel("TestClient", "Test", null, false, scriptRunner);

        _lastSettingEvent = null;
        _client.RegisterEventAction(e =>
        {
            _lastSettingEvent = e;
            return Task.FromResult<object>(Task.CompletedTask);
        });

        _sut.SettingClients.Add(_client);
    }

    #region Helper Methods

    private static SettingDefinitionDataContract CreateDataContract(string name, Type valueType,
        SettingValueBaseDataContract? value = null,
        List<string>? validValues = null,
        DataGridDefinitionDataContract? dataGridDefinition = null)
    {
        return new SettingDefinitionDataContract(
            name,
            "Test setting",
            value,
            false,
            valueType,
            null,
            null,
            null,
            validValues,
            null,
            null,
            false,
            null,
            null,
            null,
            dataGridDefinition);
    }

    private void AddSetting(ISetting setting)
    {
        _client.Settings.Add(setting);
    }

    private void Apply(string settingName, string? rawValue)
    {
        _sut.ApplyPendingValueFromCompare("TestClient", null, settingName, rawValue);
    }

    #endregion

    #region Client / Setting Lookup

    [Test]
    public void ShallThrowWhenClientNotFound()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _sut.ApplyPendingValueFromCompare("NonExistent", null, "AnySetting", "value"));
    }

    [Test]
    public void ShallThrowWhenSettingNotFound()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Apply("NonExistentSetting", "value"));
    }

    #endregion

    #region String Settings

    [Test]
    public void ShallApplyStringValue()
    {
        var setting = new StringSettingConfigurationModel(
            CreateDataContract("MySetting", typeof(string), new StringSettingDataContract("initial")),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("MySetting", "updated");

        Assert.That(setting.Value, Is.EqualTo("updated"));
    }

    [Test]
    public void ShallApplyEmptyStringAsNullForNullableString()
    {
        var setting = new StringSettingConfigurationModel(
            CreateDataContract("MySetting", typeof(string), new StringSettingDataContract("initial")),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("MySetting", "");

        Assert.That(setting.Value, Is.Null);
    }

    [Test]
    public void ShallApplyNullForNullableString()
    {
        var setting = new StringSettingConfigurationModel(
            CreateDataContract("MySetting", typeof(string), new StringSettingDataContract("initial")),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("MySetting", null);

        Assert.That(setting.Value, Is.Null);
    }

    #endregion

    #region Int Settings

    [Test]
    public void ShallApplyIntValue()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", "42");

        Assert.That(setting.Value, Is.EqualTo(42));
    }

    [Test]
    public void ShallApplyNegativeIntValue()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", "-100");

        Assert.That(setting.Value, Is.EqualTo(-100));
    }

    [Test]
    public void ShallApplyNullForNullableInt()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(10)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", null);

        Assert.That(setting.Value, Is.Null);
    }

    [Test]
    public void ShallShowErrorForInvalidInt()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(10)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", "not_a_number");

        Assert.That(setting.Value, Is.EqualTo(10));
        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.EventType, Is.EqualTo(SettingEventType.ShowErrorNotification));
        Assert.That(_lastSettingEvent.Name, Is.EqualTo("IntSetting"));
    }

    [Test]
    public void ShallShowErrorForOverflowInt()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(10)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", "99999999999999999999");

        Assert.That(setting.Value, Is.EqualTo(10));
        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.EventType, Is.EqualTo(SettingEventType.ShowErrorNotification));
    }

    #endregion

    #region Long Settings

    [Test]
    public void ShallApplyLongValue()
    {
        var setting = new LongSettingConfigurationModel(
            CreateDataContract("LongSetting", typeof(long), new LongSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("LongSetting", "9999999999");

        Assert.That(setting.Value, Is.EqualTo(9999999999L));
    }

    [Test]
    public void ShallApplyNullForNullableLong()
    {
        var setting = new LongSettingConfigurationModel(
            CreateDataContract("LongSetting", typeof(long), new LongSettingDataContract(5)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("LongSetting", "");

        Assert.That(setting.Value, Is.Null);
    }

    [Test]
    public void ShallShowErrorForInvalidLong()
    {
        var setting = new LongSettingConfigurationModel(
            CreateDataContract("LongSetting", typeof(long), new LongSettingDataContract(5)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("LongSetting", "abc");

        Assert.That(setting.Value, Is.EqualTo(5));
        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.EventType, Is.EqualTo(SettingEventType.ShowErrorNotification));
    }

    #endregion

    #region Double Settings

    [Test]
    public void ShallApplyDoubleValue()
    {
        var setting = new DoubleSettingConfigurationModel(
            CreateDataContract("DoubleSetting", typeof(double), new DoubleSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DoubleSetting", "3.14");

        Assert.That(setting.Value, Is.EqualTo(3.14).Within(0.001));
    }

    [Test]
    public void ShallApplyNegativeDoubleValue()
    {
        var setting = new DoubleSettingConfigurationModel(
            CreateDataContract("DoubleSetting", typeof(double), new DoubleSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DoubleSetting", "-2.5");

        Assert.That(setting.Value, Is.EqualTo(-2.5).Within(0.001));
    }

    [Test]
    public void ShallApplyNullForNullableDouble()
    {
        var setting = new DoubleSettingConfigurationModel(
            CreateDataContract("DoubleSetting", typeof(double), new DoubleSettingDataContract(1.5)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DoubleSetting", null);

        Assert.That(setting.Value, Is.Null);
    }

    [Test]
    public void ShallShowErrorForInvalidDouble()
    {
        var setting = new DoubleSettingConfigurationModel(
            CreateDataContract("DoubleSetting", typeof(double), new DoubleSettingDataContract(1.5)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DoubleSetting", "not_a_double");

        Assert.That(setting.Value, Is.EqualTo(1.5));
        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.EventType, Is.EqualTo(SettingEventType.ShowErrorNotification));
    }

    #endregion

    #region Bool Settings

    [Test]
    public void ShallApplyBoolTrueValue()
    {
        var setting = new BoolSettingConfigurationModel(
            CreateDataContract("BoolSetting", typeof(bool), new BoolSettingDataContract(false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("BoolSetting", "True");

        Assert.That(setting.Value, Is.True);
    }

    [Test]
    public void ShallApplyBoolFalseValue()
    {
        var setting = new BoolSettingConfigurationModel(
            CreateDataContract("BoolSetting", typeof(bool), new BoolSettingDataContract(true)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("BoolSetting", "False");

        Assert.That(setting.Value, Is.False);
    }

    [Test]
    public void ShallApplyBoolCaseInsensitive()
    {
        var setting = new BoolSettingConfigurationModel(
            CreateDataContract("BoolSetting", typeof(bool), new BoolSettingDataContract(false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("BoolSetting", "true");

        Assert.That(setting.Value, Is.True);
    }

    [Test]
    public void ShallShowErrorForEmptyBool()
    {
        var setting = new BoolSettingConfigurationModel(
            CreateDataContract("BoolSetting", typeof(bool), new BoolSettingDataContract(true)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("BoolSetting", "");

        Assert.That(setting.Value, Is.True);
        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.EventType, Is.EqualTo(SettingEventType.ShowErrorNotification));
    }

    [Test]
    public void ShallShowErrorForInvalidBool()
    {
        var setting = new BoolSettingConfigurationModel(
            CreateDataContract("BoolSetting", typeof(bool), new BoolSettingDataContract(false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("BoolSetting", "notabool");

        Assert.That(setting.Value, Is.False);
        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.EventType, Is.EqualTo(SettingEventType.ShowErrorNotification));
    }

    #endregion

    #region DateTime Settings

    [Test]
    public void ShallApplyDateTimeValue()
    {
        var setting = new DateTimeSettingConfigurationModel(
            CreateDataContract("DtSetting", typeof(DateTime), new DateTimeSettingDataContract(DateTime.MinValue)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DtSetting", "2025-06-15T10:30:00");

        Assert.That(setting.Value, Is.EqualTo(new DateTime(2025, 6, 15, 10, 30, 0)));
    }

    [Test]
    public void ShallApplyNullForNullableDateTime()
    {
        var setting = new DateTimeSettingConfigurationModel(
            CreateDataContract("DtSetting", typeof(DateTime), new DateTimeSettingDataContract(DateTime.Now)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DtSetting", null);

        Assert.That(setting.Value, Is.Null);
    }

    [Test]
    public void ShallShowErrorForInvalidDateTime()
    {
        var initialDate = new DateTime(2025, 1, 1);
        var setting = new DateTimeSettingConfigurationModel(
            CreateDataContract("DtSetting", typeof(DateTime), new DateTimeSettingDataContract(initialDate)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DtSetting", "not-a-date");

        Assert.That(setting.Value, Is.EqualTo(initialDate));
        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.EventType, Is.EqualTo(SettingEventType.ShowErrorNotification));
    }

    #endregion

    #region TimeSpan Settings

    [Test]
    public void ShallApplyTimeSpanValue()
    {
        var setting = new TimeSpanSettingConfigurationModel(
            CreateDataContract("TsSetting", typeof(TimeSpan), new TimeSpanSettingDataContract(TimeSpan.Zero)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("TsSetting", "01:30:00");

        Assert.That(setting.Value, Is.EqualTo(TimeSpan.FromMinutes(90)));
    }

    [Test]
    public void ShallApplyTimeSpanWithDays()
    {
        var setting = new TimeSpanSettingConfigurationModel(
            CreateDataContract("TsSetting", typeof(TimeSpan), new TimeSpanSettingDataContract(TimeSpan.Zero)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("TsSetting", "2.05:30:00");

        Assert.That(setting.Value, Is.EqualTo(new TimeSpan(2, 5, 30, 0)));
    }

    [Test]
    public void ShallApplyNullForNullableTimeSpan()
    {
        var setting = new TimeSpanSettingConfigurationModel(
            CreateDataContract("TsSetting", typeof(TimeSpan), new TimeSpanSettingDataContract(TimeSpan.FromHours(1))),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("TsSetting", null);

        Assert.That(setting.Value, Is.Null);
    }

    [Test]
    public void ShallShowErrorForInvalidTimeSpan()
    {
        var initial = TimeSpan.FromHours(1);
        var setting = new TimeSpanSettingConfigurationModel(
            CreateDataContract("TsSetting", typeof(TimeSpan), new TimeSpanSettingDataContract(initial)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("TsSetting", "not-a-timespan");

        Assert.That(setting.Value, Is.EqualTo(initial));
        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.EventType, Is.EqualTo(SettingEventType.ShowErrorNotification));
    }

    #endregion

    #region DropDown (String with ValidValues) Settings

    [Test]
    public void ShallApplyDropDownValue()
    {
        var setting = new DropDownSettingConfigurationModel(
            CreateDataContract("DropDown", typeof(string), new StringSettingDataContract("OptionA"),
                validValues: new List<string> { "OptionA", "OptionB", "OptionC" }),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DropDown", "OptionB");

        Assert.That(setting.Value, Is.EqualTo("OptionB"));
    }

    #endregion

    #region JSON Settings

    [Test]
    public void ShallApplyJsonStringValue()
    {
        var setting = new JsonSettingConfigurationModel(
            CreateDataContract("JsonSetting", typeof(string), new StringSettingDataContract("{}")),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("JsonSetting", "{\"key\":\"value\"}");

        Assert.That(setting.Value, Is.EqualTo("{\"key\":\"value\"}"));
    }

    [Test]
    public void ShallApplyNullForJsonSetting()
    {
        var setting = new JsonSettingConfigurationModel(
            CreateDataContract("JsonSetting", typeof(string), new StringSettingDataContract("{}")),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("JsonSetting", null);

        Assert.That(setting.Value, Is.Null);
    }

    #endregion

    #region DataGrid Settings

    [Test]
    public void ShallApplyDataGridFromValidJson()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string)),
            new("Age", typeof(int))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        var json = "[{\"Name\":\"Alice\",\"Age\":30},{\"Name\":\"Bob\",\"Age\":25}]";
        Apply("Grid", json);

        Assert.That(setting.Value, Is.Not.Null);
        Assert.That(setting.Value!.Count, Is.EqualTo(2));
        Assert.That(setting.Value[0]["Name"].ReadOnlyValue, Is.EqualTo("Alice"));
        Assert.That(setting.Value[0]["Age"].ReadOnlyValue, Is.EqualTo(30));
        Assert.That(setting.Value[1]["Name"].ReadOnlyValue, Is.EqualTo("Bob"));
    }

    [Test]
    public void ShallApplyEmptyDataGridFromNull()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string)),
            new("Value", typeof(int))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", null);

        Assert.That(setting.Value, Is.Not.Null);
        Assert.That(setting.Value!.Count, Is.EqualTo(0));
    }

    [Test]
    public void ShallApplyEmptyDataGridFromInvalidJson()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", "this is not json");

        Assert.That(setting.Value, Is.Not.Null);
        Assert.That(setting.Value!.Count, Is.EqualTo(0));
    }

    [Test]
    public void ShallApplyDataGridWithBoolColumn()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Active", typeof(bool))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", "[{\"Active\":true},{\"Active\":false}]");

        Assert.That(setting.Value!.Count, Is.EqualTo(2));
        Assert.That(setting.Value[0]["Active"].ReadOnlyValue, Is.EqualTo(true));
        Assert.That(setting.Value[1]["Active"].ReadOnlyValue, Is.EqualTo(false));
    }

    [Test]
    public void ShallApplyDataGridWithDoubleColumn()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Price", typeof(double))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", "[{\"Price\":19.99}]");

        Assert.That(setting.Value!.Count, Is.EqualTo(1));
        Assert.That((double)setting.Value[0]["Price"].ReadOnlyValue!, Is.EqualTo(19.99).Within(0.01));
    }

    [Test]
    public void ShallApplyDataGridWithLongColumn()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("BigId", typeof(long))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", "[{\"BigId\":9999999999}]");

        Assert.That(setting.Value!.Count, Is.EqualTo(1));
        Assert.That(setting.Value[0]["BigId"].ReadOnlyValue, Is.Not.Null);
    }

    [Test]
    public void ShallApplyDataGridWithDateTimeColumn()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Created", typeof(DateTime))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", "[{\"Created\":\"2025-06-15T10:30:00\"}]");

        Assert.That(setting.Value!.Count, Is.EqualTo(1));
        Assert.That(setting.Value[0]["Created"].ReadOnlyValue, Is.TypeOf<DateTime>());
    }

    [Test]
    public void ShallApplyDataGridWithTimeSpanColumn()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Duration", typeof(TimeSpan))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", "[{\"Duration\":\"01:30:00\"}]");

        Assert.That(setting.Value!.Count, Is.EqualTo(1));
        Assert.That(setting.Value[0]["Duration"].ReadOnlyValue, Is.EqualTo(TimeSpan.FromMinutes(90)));
    }

    [Test]
    public void ShallApplyDataGridWithDropDownColumn()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Status", typeof(string), validValues: new List<string> { "Active", "Inactive" })
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", "[{\"Status\":\"Active\"}]");

        Assert.That(setting.Value!.Count, Is.EqualTo(1));
        Assert.That(setting.Value[0]["Status"].ReadOnlyValue, Is.EqualTo("Active"));
    }

    [Test]
    public void ShallApplyDataGridWithMultipleColumnTypes()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string)),
            new("Count", typeof(int)),
            new("Enabled", typeof(bool)),
            new("Rate", typeof(double)),
            new("Id", typeof(long))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        var json = "[{\"Name\":\"Test\",\"Count\":5,\"Enabled\":true,\"Rate\":1.5,\"Id\":12345}]";
        Apply("Grid", json);

        Assert.That(setting.Value!.Count, Is.EqualTo(1));
        var row = setting.Value[0];
        Assert.That(row["Name"].ReadOnlyValue, Is.EqualTo("Test"));
        Assert.That(row["Count"].ReadOnlyValue, Is.EqualTo(5));
        Assert.That(row["Enabled"].ReadOnlyValue, Is.EqualTo(true));
    }

    [Test]
    public void ShallApplyEmptyDataGridFromEmptyString()
    {
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string))
        };

        var setting = new DataGridSettingConfigurationModel(
            CreateDataContract("Grid", typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("Grid", "");

        Assert.That(setting.Value, Is.Not.Null);
        Assert.That(setting.Value!.Count, Is.EqualTo(0));
    }

    #endregion

    #region Error Notification Content

    [Test]
    public void ShallIncludeSettingNameInErrorNotification()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("MySpecialInt", typeof(int), new IntSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("MySpecialInt", "abc");

        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.Name, Is.EqualTo("MySpecialInt"));
    }

    [Test]
    public void ShallIncludeTypeNameInErrorMessage()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", "abc");

        Assert.That(_lastSettingEvent, Is.Not.Null);
        Assert.That(_lastSettingEvent!.Message, Does.Contain("Int32"));
    }

    [Test]
    public void ShallNotFireErrorNotificationOnSuccess()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", "42");

        Assert.That(_lastSettingEvent == null ||
                    _lastSettingEvent.EventType != SettingEventType.ShowErrorNotification, Is.True);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void ShallApplyZeroToInt()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(5)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", "0");

        Assert.That(setting.Value, Is.EqualTo(0));
    }

    [Test]
    public void ShallApplyMaxIntValue()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", int.MaxValue.ToString());

        Assert.That(setting.Value, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void ShallApplyMinIntValue()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", int.MinValue.ToString());

        Assert.That(setting.Value, Is.EqualTo(int.MinValue));
    }

    [Test]
    public void ShallApplyWhitespaceAsNullForNullableTypes()
    {
        var setting = new IntSettingConfigurationModel(
            CreateDataContract("IntSetting", typeof(int), new IntSettingDataContract(10)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("IntSetting", "   ");

        Assert.That(setting.Value, Is.Null);
    }

    [Test]
    public void ShallApplyDoubleWithScientificNotation()
    {
        var setting = new DoubleSettingConfigurationModel(
            CreateDataContract("DoubleSetting", typeof(double), new DoubleSettingDataContract(0)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DoubleSetting", "1.5E2");

        Assert.That(setting.Value, Is.EqualTo(150.0).Within(0.001));
    }

    [Test]
    public void ShallHandleInstancedClient()
    {
        var scriptRunner = Mock.Of<IScriptRunner>();
        var instancedClient = new SettingClientConfigurationModel("ClientB", "Test", "instance1", false, scriptRunner);
        instancedClient.RegisterEventAction(e => Task.FromResult<object>(Task.CompletedTask));
        _sut.SettingClients.Add(instancedClient);

        var setting = new StringSettingConfigurationModel(
            CreateDataContract("MySetting", typeof(string), new StringSettingDataContract("old")),
            instancedClient, new SettingPresentation(false));
        instancedClient.Settings.Add(setting);

        _sut.ApplyPendingValueFromCompare("ClientB", "instance1", "MySetting", "new");

        Assert.That(setting.Value, Is.EqualTo("new"));
    }

    [Test]
    public void ShallApplyDateTimeWithDateOnly()
    {
        var setting = new DateTimeSettingConfigurationModel(
            CreateDataContract("DtSetting", typeof(DateTime), new DateTimeSettingDataContract(DateTime.MinValue)),
            _client, new SettingPresentation(false));
        AddSetting(setting);

        Apply("DtSetting", "2025-12-25");

        Assert.That(setting.Value!.Value.Year, Is.EqualTo(2025));
        Assert.That(setting.Value.Value.Month, Is.EqualTo(12));
        Assert.That(setting.Value.Value.Day, Is.EqualTo(25));
    }

    #endregion
}