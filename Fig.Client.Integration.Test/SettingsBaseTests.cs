using System.Collections.Generic;
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
}