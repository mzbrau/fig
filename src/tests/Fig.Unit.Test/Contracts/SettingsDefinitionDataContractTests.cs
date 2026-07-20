using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Scheduling;
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

    [Test]
    public void ShallRoundTripClientsPayloadWithFigHttp()
    {
        var dataGridValue = new List<Dictionary<string, object?>>
        {
            new() { { "Name", "row-1" }, { "Count", 3 } }
        };
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string)),
            new("Count", typeof(int))
        };

        var settings = new List<SettingDefinitionDataContract>
        {
            new("StringSetting",
                "A setting",
                new StringSettingDataContract("hello"),
                false,
                typeof(string),
                displayScript: "one.Hidden = false;"),
            new("IntSetting",
                "An int",
                new IntSettingDataContract(42),
                false,
                typeof(int)),
            new("GridSetting",
                "A grid",
                new DataGridSettingDataContract(dataGridValue),
                false,
                typeof(List<Dictionary<string, object>>),
                dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
            new("NullOptional",
                "Has nulls",
                new StringSettingDataContract(null),
                false,
                typeof(string),
                jsonSchema: null,
                displayScript: null)
        };

        var dataContract = new SettingsClientDefinitionDataContract(
            "TestClient",
            description: null,
            instance: null,
            hasDisplayScripts: true,
            settings,
            new List<SettingDataContract>());

        // API serializes with FigHttp; Web deserializes with FigHttp.
        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigHttp);
        Assert.That(json, Does.Not.Contain("\"Description\":null"), "NullValueHandling.Ignore should omit nulls");

        var deserialized = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(json, JsonSettings.FigHttp);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Name, Is.EqualTo("TestClient"));
        Assert.That(deserialized.Settings.Count, Is.EqualTo(4));

        Assert.That(deserialized.Settings[0].Value, Is.InstanceOf<StringSettingDataContract>());
        Assert.That(((StringSettingDataContract)deserialized.Settings[0].Value!).Value, Is.EqualTo("hello"));
        Assert.That(deserialized.Settings[0].DisplayScript, Is.EqualTo("one.Hidden = false;"));

        Assert.That(deserialized.Settings[1].Value, Is.InstanceOf<IntSettingDataContract>());
        Assert.That(((IntSettingDataContract)deserialized.Settings[1].Value!).Value, Is.EqualTo(42));

        Assert.That(deserialized.Settings[2].Value, Is.InstanceOf<DataGridSettingDataContract>());
        Assert.That(deserialized.Settings[2].DataGridDefinition, Is.Not.Null);
    }

    [Test]
    public void FigHttp_DeserializesFigDefaultPayloadWithTypeMetadata()
    {
        // Fig.Client and FigHttp both use TypeNameHandling.Objects; roundtrip must preserve $type.
        var dataContract = new SettingsClientDefinitionDataContract(
            "ClientFromFigClient",
            "desc",
            null,
            false,
            [
                new SettingDefinitionDataContract(
                    "S",
                    "d",
                    new StringSettingDataContract("v"),
                    false,
                    typeof(string))
            ],
            new List<SettingDataContract>());

        var objectsJson = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);
        var deserialized = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(
            objectsJson, JsonSettings.FigHttp);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Settings[0].Value, Is.InstanceOf<StringSettingDataContract>());
        Assert.That(((StringSettingDataContract)deserialized.Settings[0].Value!).Value, Is.EqualTo("v"));
    }

    [Test]
    public void FigHttp_SerializesIEnumerableSelectWithoutLinqIteratorType()
    {
        // Regression: TypeNameHandling.Auto emitted $type for ListSelectIterator and broke Web deserialize.
        var items = new List<DeferredChangeDataContract>
        {
            new(Guid.NewGuid(), DateTime.UtcNow, "user", "client", null, null)
        };

        var contract = new SchedulingChangesDataContract
        {
            Changes = items.Select(c => c)
        };

        var json = JsonConvert.SerializeObject(contract, JsonSettings.FigHttp);
        Assert.That(json, Does.Not.Contain("ListSelectIterator"));
        Assert.That(json, Does.Not.Contain("System.Linq"));

        var deserialized = JsonConvert.DeserializeObject<SchedulingChangesDataContract>(json, JsonSettings.FigHttp);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Changes.Count(), Is.EqualTo(1));
        Assert.That(deserialized.Changes.Single().ClientName, Is.EqualTo("client"));
    }

    [Test]
    public void FigHttp_UsesTypeNameHandlingObjects()
    {
        Assert.That(JsonSettings.FigHttp.TypeNameHandling, Is.EqualTo(TypeNameHandling.Objects));
        Assert.That(JsonSettings.FigDefault.TypeNameHandling, Is.EqualTo(TypeNameHandling.Objects));
    }

    [Test]
    public void FigHttp_EmitsShortAssemblyNamesInTypeMetadata()
    {
        var dataContract = new SettingsClientDefinitionDataContract(
            "ShortTypeClient",
            description: null,
            instance: null,
            hasDisplayScripts: false,
            [
                new SettingDefinitionDataContract(
                    "S",
                    "d",
                    new StringSettingDataContract("v"),
                    false,
                    typeof(string))
            ],
            new List<SettingDataContract>());

        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigHttp);

        Assert.That(json, Does.Contain("$type"));
        Assert.That(json, Does.Not.Contain("Version="));
        Assert.That(json, Does.Not.Contain("PublicKeyToken="));
        Assert.That(json, Does.Not.Contain("Culture="));
        Assert.That(JsonSettings.FigHttp.TypeNameAssemblyFormatHandling,
            Is.EqualTo(TypeNameAssemblyFormatHandling.Simple));

        var deserialized = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(
            json, JsonSettings.FigHttp);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Settings[0].Value, Is.InstanceOf<StringSettingDataContract>());
    }

    [Test]
    public void FigDefault_ValueTypeStillUsesFullAssemblyQualifiedName()
    {
        // FigDefault must keep full Type AQNs for DB compatibility; only FigHttp shortens them.
        var dataContract = new SettingsClientDefinitionDataContract(
            "DefaultClient",
            "desc",
            null,
            false,
            [
                new SettingDefinitionDataContract(
                    "S",
                    "d",
                    new StringSettingDataContract("v"),
                    false,
                    typeof(string))
            ],
            new List<SettingDataContract>());

        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);
        Assert.That(json, Does.Contain("Version="));
        Assert.That(json, Does.Contain("PublicKeyToken="));
    }
}