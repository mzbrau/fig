using System.Collections.Generic;
using Fig.Contracts.SettingDefinitions;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Contracts.Unit.Test;

public class SettingsDefinitionDataContractTests
{
    [Test]
    public void ShallSerializeAndDeserialize()
    {
        var dataContract = new SettingsClientDefinitionDataContract
        {
            Name = "Test",
            Settings = new List<SettingDefinitionDataContract>()
            {
                new()
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
                new()
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

        var json = JsonConvert.SerializeObject(dataContract);

        var serializedDataContract = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract>(json);

        serializedDataContract.Should().BeEquivalentTo(dataContract);
    }
}