using System.Collections.Generic;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingTypes;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Contracts.Unit.Test;

public class SettingsDefinitionDataContractTests
{
    [Test]
    public void ShallSerializeAndDeserialize()
    {
        var dataContract = new SettingsDefinitionDataContract
        {
            ServiceName = "Test",
            ServiceSecret = "Secret",
            Settings = new List<ISettingDefinition>()
            {
                new SettingDefinitionDataContract<StringType>()
                {
                    Name = "String Setting",
                    DefaultValue = "Default",
                    Description = "A setting",
                    FriendlyName = "String Setting",
                    Group = "Group",
                    IsSecret = true,
                    ValidationExplanation = "Should be valid",
                    ValidationRegex = @"\d"
                },
                new SettingDefinitionDataContract<IntType>()
                {
                    Name = "Int Setting",
                    DefaultValue = 2,
                    Description = "An int setting",
                    FriendlyName = "Int Setting",
                    Group = "Group 2",
                    IsSecret = false,
                    ValidationExplanation = "Should be valid 2",
                    ValidationRegex = @".\d"
                }
            }
        };

        var settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        
        var json = JsonConvert.SerializeObject(dataContract, settings);

        var serializedDataContract = JsonConvert.DeserializeObject<SettingsDefinitionDataContract>(json, settings);

        serializedDataContract.Should().BeEquivalentTo(dataContract);
    }
}