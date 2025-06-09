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
}