using System.Collections.Generic;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using FluentAssertions;
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
                true,
                null,
                "Default",
                typeof(string),
                ValidationType.Custom,
                @"\d",
                "Should be valid",
                group: "Group"),
            new("Int Setting",
                "An int setting",
                false,
                null,
                "Default",
                typeof(int),
                ValidationType.Custom,
                @".\d",
                "Should be valid 2",
                group: "Group 2")
        };

        var dataContract = new SettingsClientDefinitionDataContract("Test", 
            null, 
            settings,
            new List<SettingPluginVerificationDefinitionDataContract>(),
            new List<SettingDynamicVerificationDefinitionDataContract>());

        var json = JsonConvert.SerializeObject(dataContract);

        var serializedDataContract = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(json);

        serializedDataContract.Should().BeEquivalentTo(dataContract);
    }
}