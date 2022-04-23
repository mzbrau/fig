using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fig.Client.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Integration.Test.Api.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

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
        var settings = await RegisterSettings<ThreeSettings>();
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
        var settings = await RegisterSettings<ThreeSettings>();
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
        var settings = await RegisterSettings<AllSettingsAndTypes>();
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.StringSetting),
                Value = "Some value"
            },
            new()
            {
                Name = nameof(settings.IntSetting),
                Value = 77
            },
            new()
            {
                Name = nameof(settings.LongSetting),
                Value = 99L
            },
            new()
            {
                Name = nameof(settings.DateTimeSetting),
                Value = new DateTime(2000, 1, 1)
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
                Value = new List<Dictionary<string, object>>
                {
                    new()
                    {
                        {"Values", "dog"}
                    },
                    new()
                    {
                        {"Values", "cat"}
                    }
                }
            },
            new()
            {
                Name = nameof(settings.KvpCollectionSetting),
                Value = JsonConvert.SerializeObject(new List<KeyValuePair<string, string>>
                {
                    new("a", "b"),
                    new("c", "d")
                })
            },
            new()
            {
                Name = nameof(settings.ObjectListSetting),
                Value = new List<Dictionary<string, object>>
                {
                    new()
                    {
                        {nameof(SomeSetting.Key), "a"},
                        {nameof(SomeSetting.Value), "b"}
                    },
                    new()
                    {
                        {nameof(SomeSetting.Key), "c"},
                        {nameof(SomeSetting.Value), "d"}
                    }
                }
            }
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);

        Assert.That(updatedSettings.Count, Is.EqualTo(11));
        foreach (var setting in updatedSettings)
        {
            var originalSetting = settingsToUpdate.FirstOrDefault(a => a.Name == setting.Name);
            Assert.That(setting.Value.GetType(), Is.EqualTo(originalSetting.Value.GetType()),
                $"Setting {setting.Name} should have the same type");
            if (originalSetting.GetType().IsSupportedBaseType())
                Assert.That(JsonConvert.SerializeObject(setting.Value),
                    Is.EqualTo(JsonConvert.SerializeObject(originalSetting.Value)));
            else
                Assert.That(setting.Value, Is.EqualTo(originalSetting.Value),
                    $"Setting {setting.Name} should have been updated");
        }
    }

    [Test]
    public async Task ShallCreateNewSettingsInstanceWhenRequested()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var updatedSettings = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = "some new value"
            }
        };

        await SetSettings(settings.ClientName, updatedSettings, "Instance1");

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(2));
        Assert.That(clients.All(a => a.Name == settings.ClientName));
    }

    [Test]
    public async Task ShallUpdateSettingsForSpecificInstanceOnly()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var oldValue = "Horse";
        const string newValue = "A new value";
        var updatedSettings = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = newValue
            }
        };

        const string instanceName = "Instance1";
        await SetSettings(settings.ClientName, updatedSettings, instanceName);

        var noInstanceSettings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);
        var noInstance = new ThreeSettings();
        noInstance.Initialize(noInstanceSettings);

        var instanceSettings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret, instanceName);
        var instance = new ThreeSettings();
        instance.Initialize(instanceSettings);

        Assert.That(noInstance.AStringSetting, Is.EqualTo(oldValue));
        Assert.That(instance.AStringSetting, Is.EqualTo(newValue));
        Assert.That(instance.AnIntSetting, Is.EqualTo(noInstance.AnIntSetting));
    }

    [Test]
    public async Task ShallHandleMultipleInstances()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var oldValue = "Horse";
        const string newValue1 = "A new value";
        var updatedSettings1 = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = newValue1
            }
        };

        const string instance1Name = "Instance1";
        await SetSettings(settings.ClientName, updatedSettings1, instance1Name);

        const string newValue2 = "A second new value";
        var updatedSettings2 = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = newValue2
            }
        };

        const string instance2Name = "Instance2";
        await SetSettings(settings.ClientName, updatedSettings2, instance2Name);

        var noInstanceSettings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);
        var noInstance = new ThreeSettings();
        noInstance.Initialize(noInstanceSettings);

        var instance1Settings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret, instance1Name);
        var instance1 = new ThreeSettings();
        instance1.Initialize(instance1Settings);

        var instance2Settings = await GetSettingsForClient(settings.ClientName, settings.ClientSecret, instance2Name);
        var instance2 = new ThreeSettings();
        instance2.Initialize(instance2Settings);

        Assert.That(noInstance.AStringSetting, Is.EqualTo(oldValue));
        Assert.That(instance1.AStringSetting, Is.EqualTo(newValue1));
        Assert.That(instance2.AStringSetting, Is.EqualTo(newValue2));
    }

    [Test]
    public async Task ShallReturnErrorWhenSettingSettingsForNonMatchingClient()
    {
        await RegisterSettings<ThreeSettings>();

        var updatedSettings = new List<SettingDataContract>
        {
            new()
            {
                Name = "Some setting",
                Value = "some new value"
            }
        };

        var json = JsonConvert.SerializeObject(updatedSettings);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = $"/clients/{HttpUtility.UrlEncode("someUnknownClient")}/settings";
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var result = await httpClient.PutAsync(requestUri, data);

        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ShallReturnErrorWhenSettingInvalidValueForSetting()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AnIntSetting),
                Value = "This is a string"
            }
        };

        var json = JsonConvert.SerializeObject(settingsToUpdate);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = $"/clients/{HttpUtility.UrlEncode(settings.ClientName)}/settings";
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var result = await httpClient.PutAsync(requestUri, data);

        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ShallIgnoreNonMatchingSettings()
    {
        var settings = await RegisterSettings<ThreeSettings>();
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

    [Test]
    public async Task ShallOnlyReturnNonEncryptedSecretSettingsToOwningClient()
    {
        var settings = await RegisterSettings<SecretSettings>();

        var clients = await GetAllClients();
        var matchingClient = clients.FirstOrDefault(a => a.Name == settings.ClientName);
        Assert.That(GetSettingDefinitionValue(matchingClient.Settings, nameof(settings.SecretNoDefault)), Is.Null);
        Assert.That(GetSettingDefinitionValue(matchingClient.Settings, nameof(settings.SecretWithDefault)),
            Is.Not.EqualTo("cat"));

        const string noSecretValue = "one";
        const string secret1Value = "two";
        const string secret2Value = "three";
        const int secret3Value = 4;
        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.NoSecret),
                Value = noSecretValue
            },
            new()
            {
                Name = nameof(settings.SecretNoDefault),
                Value = secret1Value
            },
            new()
            {
                Name = nameof(settings.SecretWithDefault),
                Value = secret2Value
            },
            new()
            {
                Name = nameof(settings.SecretInt),
                Value = secret3Value
            }
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var clients2 = await GetAllClients();
        var matchingClient2 = clients2.FirstOrDefault(a => a.Name == settings.ClientName);
        Assert.That(GetSettingDefinitionValue(matchingClient2.Settings, nameof(settings.NoSecret)),
            Is.EqualTo(noSecretValue));
        Assert.That(GetSettingDefinitionValue(matchingClient2.Settings, nameof(settings.SecretNoDefault)),
            Is.Not.EqualTo(secret1Value));
        Assert.That(GetSettingDefinitionValue(matchingClient2.Settings, nameof(settings.SecretWithDefault)),
            Is.Not.EqualTo(secret2Value));
        Assert.That(GetSettingDefinitionValue(matchingClient2.Settings, nameof(settings.SecretInt)),
            Is.Not.EqualTo(secret3Value));

        var settingValues = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);
        Assert.That(GetSettingValue(settingValues, nameof(settings.NoSecret)), Is.EqualTo(noSecretValue));
        Assert.That(GetSettingValue(settingValues, nameof(settings.SecretNoDefault)), Is.EqualTo(secret1Value));
        Assert.That(GetSettingValue(settingValues, nameof(settings.SecretWithDefault)), Is.EqualTo(secret2Value));
        Assert.That(GetSettingValue(settingValues, nameof(settings.SecretInt)), Is.EqualTo(secret3Value));

        object? GetSettingDefinitionValue(IEnumerable<SettingDefinitionDataContract> settings, string name)
        {
            return settings.FirstOrDefault(a => a.Name == name).Value;
        }

        object? GetSettingValue(IEnumerable<SettingDataContract> settings, string name)
        {
            return settings.FirstOrDefault(a => a.Name == name).Value;
        }
    }

    [Test]
    public async Task ShallUpdateJsonValue()
    {
        var settings = await RegisterSettings<AllSettingsAndTypes>();

        var jsonValue = JsonConvert.SerializeObject(settings);

        var clients = await GetAllClients();

        var jsonSchema = clients.Single()
            .Settings.FirstOrDefault(a => a.Name == nameof(AllSettingsAndTypes.KvpCollectionSetting))?
            .JsonSchema;
        Assert.That(jsonSchema, Is.Not.Null);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.KvpCollectionSetting),
                Value = jsonValue
            }
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var settingValues = await GetSettingsForClient(settings.ClientName, settings.ClientSecret);

        var kvpSetting = settingValues.FirstOrDefault(a => a.Name == nameof(settings.KvpCollectionSetting));
        Assert.That(kvpSetting?.Value, Is.EqualTo(jsonValue));
    }
}