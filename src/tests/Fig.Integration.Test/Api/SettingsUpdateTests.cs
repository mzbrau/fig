using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Client.ExtensionMethods;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class SettingsUpdateTests : IntegrationTestBase
{
    [Test]
    public async Task ShallUpdateValue()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);

        Assert.That(updatedSettings.Count, Is.EqualTo(3));
        Assert.That(updatedSettings.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue(),
            Is.EqualTo(newValue));
    }

    [Test]
    public async Task ShallUpdateMultipleTimes()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "Some new value 2";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("intermediate value"))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);
        settingsToUpdate.First().Value = new StringSettingDataContract(newValue);
        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);

        Assert.That(updatedSettings.Count, Is.EqualTo(3));
        Assert.That(updatedSettings.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue(),
            Is.EqualTo(newValue));
    }

    [Test]
    public async Task ShallUpdateAllTypes()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<AllSettingsAndTypes>(secret);
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.StringSetting), new StringSettingDataContract("Some value")),
            new(nameof(settings.IntSetting), new IntSettingDataContract(77)),
            new(nameof(settings.LongSetting), new LongSettingDataContract(99L)),
            new(nameof(settings.DateTimeSetting), new DateTimeSettingDataContract(new DateTime(2000, 1, 1))),
            new(nameof(settings.TimespanSetting), new TimeSpanSettingDataContract(TimeSpan.FromHours(2))),
            new(nameof(settings.BoolSetting), new BoolSettingDataContract(true)),
            new(nameof(settings.LookupTableSetting), new LongSettingDataContract(10L)),
            new(nameof(settings.SecretSetting), new StringSettingDataContract("very secret password")),
            new(nameof(settings.StringCollectionSetting), new DataGridSettingDataContract(new List<Dictionary<string, object>>
            {
                new()
                {
                    {"Values", "dog"}
                },
                new()
                {
                    {"Values", "cat"}
                }
            })),
            new(nameof(settings.KvpCollectionSetting), new StringSettingDataContract(JsonConvert.SerializeObject(
                new List<KeyValuePair<string, string>>
                {
                    new("a", "b"),
                    new("c", "d")
                }))),
            new(nameof(settings.ObjectListSetting), new DataGridSettingDataContract(new List<Dictionary<string, object>>
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
            }))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);

        Assert.That(updatedSettings.Count, Is.EqualTo(11));
        foreach (var setting in updatedSettings)
        {
            var originalSetting = settingsToUpdate.First(a => a.Name == setting.Name);
            Assert.That(setting.Value.GetType(), Is.EqualTo(originalSetting.Value.GetType()),
                $"Setting {setting.Name} should have the same type");
            if (originalSetting.GetType().IsSupportedBaseType())
                Assert.That(JsonConvert.SerializeObject(setting.Value),
                    Is.EqualTo(JsonConvert.SerializeObject(originalSetting.Value)));
            else
                Assert.That(setting.Value?.GetValue(), Is.EqualTo(originalSetting.Value?.GetValue()),
                    $"Setting {setting.Name} should have been updated");
        }
    }

    [Test]
    public async Task ShallCreateNewSettingsInstanceWhenRequested()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("some new value"))
        };

        await SetSettings(settings.ClientName, updatedSettings, "Instance1");

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(2));
        Assert.That(clients.All(a => a.Name == settings.ClientName));
    }

    [Test]
    public async Task ShallUpdateSettingsForSpecificInstanceOnly()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var oldValue = "Horse";
        const string newValue = "A new value";
        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        const string instanceName = "Instance1";
        await SetSettings(settings.ClientName, updatedSettings, instanceName);

        var noInstanceSettings = await GetSettingsForClient(settings.ClientName, secret);
        var noInstance = new ThreeSettings();
        noInstance.Initialize(noInstanceSettings);

        var instanceSettings = await GetSettingsForClient(settings.ClientName, secret, instanceName);
        var instance = new ThreeSettings();
        instance.Initialize(instanceSettings);

        Assert.That(noInstance.AStringSetting, Is.EqualTo(oldValue));
        Assert.That(instance.AStringSetting, Is.EqualTo(newValue));
        Assert.That(instance.AnIntSetting, Is.EqualTo(noInstance.AnIntSetting));
    }

    [Test]
    public async Task ShallHandleMultipleInstances()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var oldValue = "Horse";
        const string newValue1 = "A new value";
        var updatedSettings1 = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue1))
        };

        const string instance1Name = "Instance1";
        await SetSettings(settings.ClientName, updatedSettings1, instance1Name);

        const string newValue2 = "A second new value";
        var updatedSettings2 = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue2))
        };

        const string instance2Name = "Instance2";
        await SetSettings(settings.ClientName, updatedSettings2, instance2Name);

        var noInstanceSettings = await GetSettingsForClient(settings.ClientName, secret);
        var noInstance = new ThreeSettings();
        noInstance.Initialize(noInstanceSettings);

        var instance1Settings = await GetSettingsForClient(settings.ClientName, secret, instance1Name);
        var instance1 = new ThreeSettings();
        instance1.Initialize(instance1Settings);

        var instance2Settings = await GetSettingsForClient(settings.ClientName, secret, instance2Name);
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
            new("Some setting", new StringSettingDataContract("some new value"))
        };

        var requestUri = $"/clients/{Uri.EscapeDataString("someUnknownClient")}/settings";

        await ApiClient.PutAndVerify(requestUri, updatedSettings, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallReturnErrorWhenSettingInvalidValueForSetting()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AnIntSetting), new StringSettingDataContract("This is a string"))
        };

        var requestUri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/settings";

        await ApiClient.PutAndVerify(requestUri, settingsToUpdate, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShallIgnoreNonMatchingSettings()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue)),
            new("Some setting that doesn't exist", new StringSettingDataContract("some random value"))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);

        Assert.That(updatedSettings.Count, Is.EqualTo(3));
        Assert.That(updatedSettings.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue(),
            Is.EqualTo(newValue));
    }

    [Test]
    public async Task ShallOnlyReturnNonEncryptedSecretSettingsToOwningClient()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SecretSettings>(secret);

        var clients = await GetAllClients();
        var matchingClient = clients.First(a => a.Name == settings.ClientName);
        Assert.That(GetSettingDefinitionValue(matchingClient.Settings, nameof(settings.SecretNoDefault)), Is.Null);
        Assert.That(GetSettingDefinitionValue(matchingClient.Settings, nameof(settings.SecretWithDefault)),
            Is.Not.EqualTo("cat"));

        const string noSecretValue = "one";
        const string secret1Value = "two";
        const string secret2Value = "three";
        const int secret3Value = 4;
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.NoSecret), new StringSettingDataContract(noSecretValue)),
            new(nameof(settings.SecretNoDefault), new StringSettingDataContract(secret1Value)),
            new(nameof(settings.SecretWithDefault), new StringSettingDataContract(secret2Value)),
            new(nameof(settings.SecretInt), new IntSettingDataContract(secret3Value))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var clients2 = await GetAllClients();
        var matchingClient2 = clients2.First(a => a.Name == settings.ClientName);
        Assert.That(GetSettingDefinitionValue(matchingClient2.Settings, nameof(settings.NoSecret)),
            Is.EqualTo(noSecretValue));
        Assert.That(GetSettingDefinitionValue(matchingClient2.Settings, nameof(settings.SecretNoDefault)),
            Is.Not.EqualTo(secret1Value));
        Assert.That(GetSettingDefinitionValue(matchingClient2.Settings, nameof(settings.SecretWithDefault)),
            Is.Not.EqualTo(secret2Value));
        Assert.That(GetSettingDefinitionValue(matchingClient2.Settings, nameof(settings.SecretInt)),
            Is.Not.EqualTo(secret3Value));

        var settingValues = await GetSettingsForClient(settings.ClientName, secret);
        Assert.That(GetSettingValue(settingValues, nameof(settings.NoSecret)), Is.EqualTo(noSecretValue));
        Assert.That(GetSettingValue(settingValues, nameof(settings.SecretNoDefault)), Is.EqualTo(secret1Value));
        Assert.That(GetSettingValue(settingValues, nameof(settings.SecretWithDefault)), Is.EqualTo(secret2Value));
        Assert.That(GetSettingValue(settingValues, nameof(settings.SecretInt)), Is.EqualTo(secret3Value));

        object? GetSettingDefinitionValue(IEnumerable<SettingDefinitionDataContract> settingCollection, string name)
        {
            return settingCollection.First(a => a.Name == name).Value?.GetValue();
        }

        object? GetSettingValue(IEnumerable<SettingDataContract> settingCollection, string name)
        {
            return settingCollection.First(a => a.Name == name).Value?.GetValue();
        }
    }

    [Test]
    public async Task ShallUpdateJsonValue()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<AllSettingsAndTypes>(secret);

        var jsonValue = JsonConvert.SerializeObject(settings);

        var clients = await GetAllClients();

        var jsonSchema = clients.Single()
            .Settings.FirstOrDefault(a => a.Name == nameof(AllSettingsAndTypes.KvpCollectionSetting))?
            .JsonSchema;
        Assert.That(jsonSchema, Is.Not.Null);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.KvpCollectionSetting), new StringSettingDataContract(jsonValue))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var settingValues = await GetSettingsForClient(settings.ClientName, secret);

        var kvpSetting = settingValues.FirstOrDefault(a => a.Name == nameof(settings.KvpCollectionSetting));
        Assert.That(kvpSetting?.Value?.GetValue(), Is.EqualTo(jsonValue));
    }
}