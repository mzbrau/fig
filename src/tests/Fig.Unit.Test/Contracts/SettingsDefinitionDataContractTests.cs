using System.Collections.Generic;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Contracts;

public class SettingsDefinitionDataContractTests
{
    [Test]
    public void ShallSerializeAndDeserialize()
    {
        var settings = new List<SettingDefinitionDataContract>()
        {
            new("String Setting",
                "A setting",
                new StringSettingDataContract("Default"),
                false,
                typeof(string),
                null,
                @"\d",
                "Should be valid",
                group: "Group"),
            new("Int Setting",
                "An int setting",
                new IntSettingDataContract(2),
                false,
                typeof(int),
                null,
                @".\d",
                "Should be valid 2",
                group: "Group 2")
        };

        var dataContract = new SettingsClientDefinitionDataContract("Test", "A description",
            null,
            false,
            settings,
            new List<SettingDataContract>());

        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);

        var serializedDataContract = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(json, JsonSettings.FigDefault);

        Assert.That(JsonConvert.SerializeObject(serializedDataContract, JsonSettings.FigDefault),
            Is.EqualTo(JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault)));
    }

    [Test]
    public void ShallSerializeAndDeserializeWithDataGridSetting()
    {
        var dataGridDefault = new List<Dictionary<string, object?>>
        {
            new() { { "Name", "my database setting" } }
        };
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string))
        };

        var settings = new List<SettingDefinitionDataContract>()
        {
            new("DatabaseSettingsList",
                "my name",
                defaultValue: new DataGridSettingDataContract(dataGridDefault),
                valueType: typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, true)),
            new("MyName",
                "my name",
                new StringSettingDataContract("Sam"),
                false,
                typeof(string),
                group: "Name")
        };

        var dataContract = new SettingsClientDefinitionDataContract("AspNetApi", "AspNetApi Example",
            null,
            false,
            settings,
            new List<SettingDataContract>());

        // Serialize with client settings (FigDefault)
        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);

        // Deserialize with API settings (TypeNameHandling.Objects only, like the API does)
        var apiSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };
        var deserialized = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(json, apiSettings);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Name, Is.EqualTo("AspNetApi"));
        Assert.That(deserialized.Settings.Count, Is.EqualTo(2));
        
        // Verify DataGrid setting survived roundtrip
        var dgSetting = deserialized.Settings[0];
        Assert.That(dgSetting.Name, Is.EqualTo("DatabaseSettingsList"));
        Assert.That(dgSetting.DefaultValue, Is.InstanceOf<DataGridSettingDataContract>());
        Assert.That(dgSetting.DataGridDefinition, Is.Not.Null);
        Assert.That(dgSetting.DataGridDefinition!.IsLocked, Is.True);
    }
}