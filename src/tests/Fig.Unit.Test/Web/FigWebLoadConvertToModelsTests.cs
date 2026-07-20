using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Json;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.Json;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

/// <summary>
/// Verifies FigWebLoad payloads still produce the correct ISetting model types
/// (the ConvertToModels path depends on ValueType.FigPropertyType + Value shape).
/// </summary>
[TestFixture]
public class FigWebLoadConvertToModelsTests
{
    [Test]
    public void AfterFigWebLoadRoundTrip_ConvertProducesExpectedSettingModels()
    {
        var gridRows = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Alice", ["Age"] = 30 }
        };
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string)),
            new("Age", typeof(int))
        };

        var source = new SettingsClientDefinitionDataContract(
            "ConvertClient",
            description: null,
            instance: null,
            hasDisplayScripts: false,
            [
                new SettingDefinitionDataContract(
                    "StringSetting", "d", new StringSettingDataContract("hello"), false, typeof(string)),
                new SettingDefinitionDataContract(
                    "DropDown", "d", new StringSettingDataContract("A"), false, typeof(string),
                    validValues: ["A", "B"]),
                new SettingDefinitionDataContract(
                    "JsonSetting", "d", new JsonSettingDataContract("{}"), false, typeof(string),
                    jsonSchema: "{}"),
                new SettingDefinitionDataContract(
                    "IntSetting", "d", new IntSettingDataContract(1), false, typeof(int)),
                new SettingDefinitionDataContract(
                    "BoolSetting", "d", new BoolSettingDataContract(true), false, typeof(bool)),
                new SettingDefinitionDataContract(
                    "LongSetting", "d", new LongSettingDataContract(2L), false, typeof(long)),
                new SettingDefinitionDataContract(
                    "DoubleSetting", "d", new DoubleSettingDataContract(1.5), false, typeof(double)),
                new SettingDefinitionDataContract(
                    "DateTimeSetting", "d",
                    new DateTimeSettingDataContract(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                    false, typeof(DateTime)),
                new SettingDefinitionDataContract(
                    "TimeSpanSetting", "d",
                    new TimeSpanSettingDataContract(TimeSpan.FromMinutes(1)),
                    false, typeof(TimeSpan)),
                new SettingDefinitionDataContract(
                    "GridSetting", "d", new DataGridSettingDataContract(gridRows), false,
                    typeof(List<Dictionary<string, object>>),
                    dataGridDefinition: new DataGridDefinitionDataContract(columns, false))
            ],
            new List<SettingDataContract>());

        var json = JsonConvert.SerializeObject(source, FigWebLoadJsonSettings.Instance);
        Assert.That(json, Does.Not.Contain("$type"));

        var client = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(
            json, FigWebLoadJsonSettings.Instance);
        Assert.That(client, Is.Not.Null);

        foreach (var setting in client!.Settings)
        {
            Assert.That(setting.ValueType.FigPropertyType(), Is.Not.EqualTo(FigPropertyType.Unsupported),
                $"ValueType for {setting.Name} must map after FigWebLoad round-trip");
        }

        var parent = new SettingClientConfigurationModel(
            client.Name, client.Description ?? string.Empty, null, false, Mock.Of<IScriptRunner>());
        var presentation = new SettingPresentation(false);

        var models = client.Settings.Select(s => ConvertSetting(s, parent, presentation)).ToList();

        Assert.That(models[0], Is.InstanceOf<StringSettingConfigurationModel>());
        Assert.That(models[1], Is.InstanceOf<DropDownSettingConfigurationModel>());
        Assert.That(models[2], Is.InstanceOf<JsonSettingConfigurationModel>());
        Assert.That(models[3], Is.InstanceOf<IntSettingConfigurationModel>());
        Assert.That(models[4], Is.InstanceOf<BoolSettingConfigurationModel>());
        Assert.That(models[5], Is.InstanceOf<LongSettingConfigurationModel>());
        Assert.That(models[6], Is.InstanceOf<DoubleSettingConfigurationModel>());
        Assert.That(models[7], Is.InstanceOf<DateTimeSettingConfigurationModel>());
        Assert.That(models[8], Is.InstanceOf<TimeSpanSettingConfigurationModel>());
        Assert.That(models[9], Is.InstanceOf<DataGridSettingConfigurationModel>());

        Assert.That(((StringSettingConfigurationModel)models[0]).Value, Is.EqualTo("hello"));
        Assert.That(((IntSettingConfigurationModel)models[3]).Value, Is.EqualTo(1));
        Assert.That(((DataGridSettingConfigurationModel)models[9]).Value, Is.Not.Null);
        Assert.That(((DataGridSettingConfigurationModel)models[9]).Value!, Has.Count.EqualTo(1));
    }

    [Test]
    public void FigHttp_ClientWire_Unchanged_StillObjects()
    {
        Assert.That(JsonSettings.FigHttp.TypeNameHandling, Is.EqualTo(TypeNameHandling.Objects));
    }

    private static ISetting ConvertSetting(
        SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent,
        SettingPresentation presentation)
    {
        return dataContract.ValueType.FigPropertyType() switch
        {
            FigPropertyType.String when dataContract.ValidValues != null =>
                new DropDownSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.String when dataContract.JsonSchema != null =>
                new JsonSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.String => new StringSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.Int => new IntSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.Long => new LongSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.Double => new DoubleSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.Bool => new BoolSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.DataGrid => new DataGridSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.DateTime => new DateTimeSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.TimeSpan => new TimeSpanSettingConfigurationModel(dataContract, parent, presentation),
            _ => throw new AssertionException(
                $"Unsupported FigPropertyType for {dataContract.Name}: {dataContract.ValueType.FigPropertyType()}")
        };
    }
}
