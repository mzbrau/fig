using System.Collections.Generic;
using System.Linq;
using Fig.Client.Attributes;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingTypes;
using NUnit.Framework;

namespace Fig.Client.Integration.Test;

[TestFixture]
public class SettingsBaseTests
{
    [Test]
    public void ShallSetDefaultValues()
    {
        var settings = new TestSettings();
        
        Assert.That(settings.StringSetting, Is.EqualTo("test"));
        Assert.That(settings.IntSetting, Is.EqualTo(4));
    }

    [Test]
    public void ShallSetValuesFromDataContract()
    {
        const string stringValue = "From data contract";
        const int intValue = 10;
        
        var dataContract = new SettingsDataContract
        {
            Settings = new List<ISetting>
            {
                new SettingDataContract<StringType>()
                {
                    Name = "StringSetting",
                    Value = stringValue
                },
                new SettingDataContract<IntType>()
                {
                    Name = "IntSetting",
                    Value = intValue
                }
            }
        };

        var settings = new TestSettings(new SettingDefinitionFactory(), dataContract);
        
        Assert.That(settings.StringSetting, Is.EqualTo(stringValue));
        Assert.That(settings.IntSetting, Is.EqualTo(intValue));
    }

    [Test]
    public void ShallConvertToDefinitionsDataContract()
    {
        var settings = new TestSettings();

        var dataContract = settings.CreateDataContract();
        
        Assert.That(dataContract.ServiceName, Is.EqualTo(settings.ServiceName));
        Assert.That(dataContract.ServiceSecret, Is.EqualTo(settings.ServiceSecret));
        Assert.That(dataContract.Settings.Count, Is.EqualTo(2));
        VerifySetting("StringSetting", "String Setting", 
            "This is a test setting", @"(.*[a-z]){3,}",
            "Must have at least 3 characters", "test", true);
        VerifySetting("IntSetting", "Int Setting", 
            "This is a test int setting", null,
            null, 4, false);

        void VerifySetting(string name, string friendlyName, string description, string? validationRegex,
            string? validationExplanation, object defaultValue, bool isSecret)
        {
            var setting = dataContract.Settings.FirstOrDefault(a => a.Name == name);
            
            Assert.That(setting, Is.Not.Null);
#pragma warning disable CS8602
            Assert.That(setting.FriendlyName, Is.EqualTo(friendlyName));
#pragma warning restore CS8602
            Assert.That(setting.Description, Is.EqualTo(description));
            Assert.That(setting.ValidationRegex, Is.EqualTo(validationRegex));
            Assert.That(setting.ValidationExplanation, Is.EqualTo(validationExplanation));
            Assert.That(setting.DefaultValue, Is.EqualTo(defaultValue));
            Assert.That(setting.IsSecret, Is.EqualTo(isSecret));
        }
    }
}