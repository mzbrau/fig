using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Api.Integration.Test.TestSettings;
using Fig.Contracts.Settings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Api.Integration.Test;

[TestFixture]
public class SettingsUpdateTests : IntegrationTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await DeleteAllClients();
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
    }

    [Test]
    public async Task ShallUpdateValue()
    {
        var settings = await RegisterThreeSettings();
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = newValue
            }
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);

        Assert.That(updatedSettings.Count, Is.EqualTo(3));
        Assert.That(updatedSettings.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting)).Value,
            Is.EqualTo(newValue));
    }

    [Test]
    public async Task ShallUpdateMultipleTimes()
    {
        var settings = await RegisterThreeSettings();
        const string newValue = "Some new value 2";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = "intermediate value"
            }
        };

        await SetSettings(settings.ClientName, settingsToUpdate);
        settingsToUpdate.First().Value = newValue;
        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);

        Assert.That(updatedSettings.Count, Is.EqualTo(3));
        Assert.That(updatedSettings.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting)).Value,
            Is.EqualTo(newValue));
    }

    [Test]
    public async Task ShallUpdateAllTypes()
    {
        var settings = await RegisterAllSettingsAndTypes();
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.StringSetting),
                Value = "Some value"
            },
            new()
            {
                Name = nameof(settings.LongSetting),
                Value = (long)77
            },
            new()
            {
                Name = nameof(settings.DateTimeSetting),
                Value = new DateTime(2000, 1,1)
            },
            new()
            {
                Name = nameof(settings.TimespanSetting),
                Value = TimeSpan.FromHours(2)
            },
            new()
            {
                Name = nameof(settings.BoolSetting),
                Value = true
            },
            new()
            {
                Name = nameof(settings.SecretSetting),
                Value = "very secret password"
            },
            new()
            {
                Name = nameof(settings.ComplexStringSetting),
                Value = "w:x,y:z"
            },
            new()
            {
                Name = nameof(settings.StringCollectionSetting),
                Value = new List<string> { "dog", "cat" }
            },
            new()
            {
                Name = nameof(settings.KvpCollectionSetting),
                Value = new List<KeyValuePair<string, string>>()
                {
                    new("a", "b"),
                    new("c", "d")
                }
            },
            new()
            {
                Name = nameof(settings.ObjectListSetting),
                Value = new List<SomeSetting>()
                {
                    new()
                    {
                        Key = "a",
                        Value = "d"
                    },
                    new()
                    {
                        Key = "h",
                        Value = "i"
                    },
                }
            }
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);

        Assert.That(updatedSettings.Count, Is.EqualTo(10));
        foreach (var setting in updatedSettings)
        {
            var originalSetting = settingsToUpdate.FirstOrDefault(a => a.Name == setting.Name);
            Assert.That(setting.Value.GetType(), Is.EqualTo(originalSetting.Value.GetType()), $"Setting {setting.Name} should have the same type");
            if (setting.Name == nameof(settings.ObjectListSetting))
            {
                Assert.That(JsonConvert.SerializeObject(setting.Value), Is.EqualTo(JsonConvert.SerializeObject(originalSetting.Value)));
            }
            else
            {
                Assert.That(setting.Value, Is.EqualTo(originalSetting.Value), $"Setting {setting.Name} should have been updated");
            }
        }
    }

    public void ShallUpdateSettingsForSpecificInstanceOnly()
    {
        
    }

    [Test]
    public async Task ShallIgnoreNonMatchingSettings()
    {
        var settings = await RegisterThreeSettings();
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = newValue
            },
            new()
            {
                Name = "Some setting that doesn't exist",
                Value = "some random value"
            }
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);

        Assert.That(updatedSettings.Count, Is.EqualTo(3));
        Assert.That(updatedSettings.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting)).Value,
            Is.EqualTo(newValue));
    }
    
    // TODO: Something around groups
}