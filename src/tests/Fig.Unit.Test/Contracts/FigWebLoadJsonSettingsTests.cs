using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Json;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingMigrations;
using Fig.Contracts.Settings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Contracts;

/// <summary>
/// Prove-first spike: FigWebLoad compact polymorphic values round-trip without $type.
/// Must stay green before GET /clients is wired to FigWebLoadJsonSettings.
/// </summary>
[TestFixture]
public class FigWebLoadJsonSettingsTests
{
    private static readonly JsonSerializerSettings Settings = FigWebLoadJsonSettings.Instance;

    [TestCaseSource(nameof(AllValueSubtypes))]
    public void SettingValue_RoundTripsConcreteType(SettingValueBaseDataContract original, Type expectedType)
    {
        var json = JsonConvert.SerializeObject(original, Settings);
        Assert.That(json, Does.Not.Contain("$type"), "compact converter must not emit $type");
        Assert.That(json, Does.Contain("\"t\":"));

        var deserialized = JsonConvert.DeserializeObject<SettingValueBaseDataContract>(json, Settings);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.GetType(), Is.EqualTo(expectedType));
        Assert.That(Normalize(deserialized.GetValue()), Is.EqualTo(Normalize(original.GetValue())));
    }

    [Test]
    public void SettingValue_Null_RoundTrips()
    {
        var json = JsonConvert.SerializeObject((SettingValueBaseDataContract?)null, Settings);
        var deserialized = JsonConvert.DeserializeObject<SettingValueBaseDataContract>(json, Settings);
        Assert.That(deserialized, Is.Null);
    }

    [Test]
    public void FullClientsPayload_RoundTripsAllShapes_WithoutTypeMetadata()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var span = TimeSpan.FromMinutes(5);
        var gridRows = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["Name"] = "Alice",
                ["Nickname"] = null,
                ["Age"] = 30
            }
        };
        var columns = new List<DataGridColumnDataContract>
        {
            new("Name", typeof(string)),
            new("Nickname", typeof(string)),
            new("Age", typeof(int))
        };

        var clients = new List<SettingsClientDefinitionDataContract>
        {
            new(
                "WebLoadClient",
                description: null,
                instance: null,
                hasDisplayScripts: false,
                [
                    new SettingDefinitionDataContract(
                        "StringSetting",
                        "d",
                        new StringSettingDataContract("hello"),
                        false,
                        typeof(string),
                        defaultValue: new StringSettingDataContract("def")),
                    new SettingDefinitionDataContract(
                        "IntSetting",
                        "d",
                        new IntSettingDataContract(42),
                        false,
                        typeof(int)),
                    new SettingDefinitionDataContract(
                        "BoolSetting",
                        "d",
                        new BoolSettingDataContract(true),
                        false,
                        typeof(bool)),
                    new SettingDefinitionDataContract(
                        "LongSetting",
                        "d",
                        new LongSettingDataContract(99L),
                        false,
                        typeof(long)),
                    new SettingDefinitionDataContract(
                        "DoubleSetting",
                        "d",
                        new DoubleSettingDataContract(3.14),
                        false,
                        typeof(double)),
                    new SettingDefinitionDataContract(
                        "DateTimeSetting",
                        "d",
                        new DateTimeSettingDataContract(now),
                        false,
                        typeof(DateTime)),
                    new SettingDefinitionDataContract(
                        "TimeSpanSetting",
                        "d",
                        new TimeSpanSettingDataContract(span),
                        false,
                        typeof(TimeSpan)),
                    new SettingDefinitionDataContract(
                        "JsonSetting",
                        "d",
                        new JsonSettingDataContract("{\"a\":1}"),
                        false,
                        typeof(string),
                        jsonSchema: "{}"),
                    new SettingDefinitionDataContract(
                        "DropDownSetting",
                        "d",
                        new StringSettingDataContract("A"),
                        false,
                        typeof(string),
                        validValues: ["A", "B"]),
                    new SettingDefinitionDataContract(
                        "DataGridSetting",
                        "d",
                        new DataGridSettingDataContract(gridRows),
                        false,
                        typeof(List<Dictionary<string, object>>),
                        dataGridDefinition: new DataGridDefinitionDataContract(columns, false)),
                    new SettingDefinitionDataContract(
                        "SecretSetting",
                        "d",
                        value: null,
                        isSecret: true,
                        valueType: typeof(string))
                ],
                clientSettingOverrides:
                [
                    new SettingDataContract("Override", new StringSettingDataContract("ov"))
                ],
                settingMigrationResults:
                [
                    new SettingMigrationResultDataContract(
                        "Old",
                        "New",
                        null,
                        new IntSettingDataContract(7),
                        "fp1")
                ])
        };

        var json = JsonConvert.SerializeObject(clients, Settings);

        Assert.That(json, Does.Not.Contain("$type"));
        Assert.That(json, Does.Not.Contain("Version="));
        Assert.That(json, Does.Not.Contain("PublicKeyToken="));
        Assert.That(json, Does.Contain("\"t\":\"s\""));
        Assert.That(json, Does.Contain("\"t\":\"dg\""));

        var deserialized = JsonConvert.DeserializeObject<List<SettingsClientDefinitionDataContract>>(json, Settings);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!, Has.Count.EqualTo(1));

        var settings = deserialized[0].Settings;
        Assert.That(settings, Has.Count.EqualTo(11));

        Assert.That(settings[0].Value, Is.InstanceOf<StringSettingDataContract>());
        Assert.That(((StringSettingDataContract)settings[0].Value!).Value, Is.EqualTo("hello"));
        Assert.That(settings[0].DefaultValue, Is.InstanceOf<StringSettingDataContract>());
        Assert.That(settings[0].ValueType, Is.EqualTo(typeof(string)));

        Assert.That(settings[1].Value, Is.InstanceOf<IntSettingDataContract>());
        Assert.That(((IntSettingDataContract)settings[1].Value!).Value, Is.EqualTo(42));
        Assert.That(settings[1].ValueType, Is.EqualTo(typeof(int)));

        Assert.That(settings[2].Value, Is.InstanceOf<BoolSettingDataContract>());
        Assert.That(((BoolSettingDataContract)settings[2].Value!).Value, Is.True);

        Assert.That(settings[3].Value, Is.InstanceOf<LongSettingDataContract>());
        Assert.That(((LongSettingDataContract)settings[3].Value!).Value, Is.EqualTo(99L));

        Assert.That(settings[4].Value, Is.InstanceOf<DoubleSettingDataContract>());
        Assert.That(((DoubleSettingDataContract)settings[4].Value!).Value, Is.EqualTo(3.14));

        Assert.That(settings[5].Value, Is.InstanceOf<DateTimeSettingDataContract>());
        Assert.That(((DateTimeSettingDataContract)settings[5].Value!).Value, Is.EqualTo(now));

        Assert.That(settings[6].Value, Is.InstanceOf<TimeSpanSettingDataContract>());
        Assert.That(((TimeSpanSettingDataContract)settings[6].Value!).Value, Is.EqualTo(span));

        Assert.That(settings[7].Value, Is.InstanceOf<JsonSettingDataContract>());
        Assert.That(((JsonSettingDataContract)settings[7].Value!).Value, Is.EqualTo("{\"a\":1}"));

        Assert.That(settings[8].Value, Is.InstanceOf<StringSettingDataContract>());
        Assert.That(settings[8].ValidValues, Is.EquivalentTo(new[] { "A", "B" }));

        Assert.That(settings[9].Value, Is.InstanceOf<DataGridSettingDataContract>());
        var grid = ((DataGridSettingDataContract)settings[9].Value!).Value!;
        Assert.That(grid[0]["Name"], Is.EqualTo("Alice"));
        Assert.That(grid[0].ContainsKey("Nickname"), Is.True);
        Assert.That(grid[0]["Nickname"], Is.Null);
        Assert.That(Convert.ToInt32(grid[0]["Age"]), Is.EqualTo(30));
        Assert.That(settings[9].DataGridDefinition!.Columns[0].ValueType, Is.EqualTo(typeof(string)));
        Assert.That(settings[9].ValueType, Is.EqualTo(typeof(List<Dictionary<string, object>>)));

        Assert.That(settings[10].IsSecret, Is.True);
        Assert.That(settings[10].Value, Is.Null);

        var overrideSetting = deserialized[0].ClientSettingOverrides.Single();
        Assert.That(overrideSetting.Value, Is.InstanceOf<StringSettingDataContract>());
        Assert.That(((StringSettingDataContract)overrideSetting.Value!).Value, Is.EqualTo("ov"));

        var migration = deserialized[0].SettingMigrationResults.Single();
        Assert.That(migration.MigratedValue, Is.InstanceOf<IntSettingDataContract>());
        Assert.That(((IntSettingDataContract)migration.MigratedValue!).Value, Is.EqualTo(7));
    }

    [Test]
    public void FigHttp_StillUsesTypeNameHandlingObjects()
    {
        Assert.That(JsonSettings.FigHttp.TypeNameHandling, Is.EqualTo(TypeNameHandling.Objects));
        Assert.That(FigWebLoadJsonSettings.Instance.TypeNameHandling, Is.EqualTo(TypeNameHandling.None));
    }

    [Test]
    public void MissingDiscriminator_Throws()
    {
        const string bad = "{\"Value\":\"x\"}";
        Assert.Throws<JsonSerializationException>(() =>
            JsonConvert.DeserializeObject<SettingValueBaseDataContract>(bad, Settings));
    }

    private static IEnumerable<TestCaseData> AllValueSubtypes()
    {
        var now = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);
        yield return new TestCaseData(new StringSettingDataContract("x"), typeof(StringSettingDataContract))
            .SetName("String");
        yield return new TestCaseData(new IntSettingDataContract(1), typeof(IntSettingDataContract))
            .SetName("Int");
        yield return new TestCaseData(new BoolSettingDataContract(false), typeof(BoolSettingDataContract))
            .SetName("Bool");
        yield return new TestCaseData(new LongSettingDataContract(2L), typeof(LongSettingDataContract))
            .SetName("Long");
        yield return new TestCaseData(new DoubleSettingDataContract(1.5), typeof(DoubleSettingDataContract))
            .SetName("Double");
        yield return new TestCaseData(new DateTimeSettingDataContract(now), typeof(DateTimeSettingDataContract))
            .SetName("DateTime");
        yield return new TestCaseData(new TimeSpanSettingDataContract(TimeSpan.FromSeconds(3)), typeof(TimeSpanSettingDataContract))
            .SetName("TimeSpan");
        yield return new TestCaseData(new JsonSettingDataContract("{}"), typeof(JsonSettingDataContract))
            .SetName("Json");
        yield return new TestCaseData(
                new DataGridSettingDataContract(
                [
                    new Dictionary<string, object?> { ["Col"] = "v" }
                ]),
                typeof(DataGridSettingDataContract))
            .SetName("DataGrid");
    }

    private static object? Normalize(object? value)
    {
        if (value is List<Dictionary<string, object?>> rows)
        {
            return rows.Select(row => row.ToDictionary(
                kv => kv.Key,
                kv => kv.Value is long l && l is >= int.MinValue and <= int.MaxValue
                    ? (object)(int)l
                    : kv.Value)).ToList();
        }

        return value;
    }
}
