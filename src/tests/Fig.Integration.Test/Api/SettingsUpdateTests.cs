using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fig.Client;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
    public void ShallSetCorrectDefaultValuesBeforeUpdate()
    {
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<AllSettingsAndTypes>(secret);

        Assert.That(settings.CurrentValue.BoolSetting, Is.True);
        Assert.That(settings.CurrentValue.DateTimeSetting, Is.Null);
        Assert.That(settings.CurrentValue.DoubleSetting, Is.EqualTo(45.3));
        Assert.That(settings.CurrentValue.EnumSetting, Is.EqualTo(Pets.Cat));
        Assert.That(settings.CurrentValue.IntSetting, Is.EqualTo(34));
        Assert.That(settings.CurrentValue.JsonSetting, Is.Null);
        Assert.That(settings.CurrentValue.LongSetting, Is.EqualTo(64));
        Assert.That(settings.CurrentValue.SecretSetting, Is.EqualTo("SecretString"));
        Assert.That(settings.CurrentValue.StringCollectionSetting, Is.Null);
        Assert.That(settings.CurrentValue.StringSetting, Is.EqualTo("Cat"));
        Assert.That(settings.CurrentValue.TimespanSetting, Is.Null);
        Assert.That(settings.CurrentValue.LookupTableSetting, Is.EqualTo(5));
        Assert.That(JsonConvert.SerializeObject(settings.CurrentValue.ObjectListSetting), Is.EqualTo(JsonConvert.SerializeObject(AllSettingsAndTypes.GetDefaultObjectList())));
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
            new(nameof(settings.DoubleSetting), new DoubleSettingDataContract(55.4)),
            new(nameof(settings.DateTimeSetting), new DateTimeSettingDataContract(new DateTime(2000, 1, 1))),
            new(nameof(settings.TimespanSetting), new TimeSpanSettingDataContract(TimeSpan.FromHours(2))),
            new(nameof(settings.BoolSetting), new BoolSettingDataContract(true)),
            new(nameof(settings.LookupTableSetting), new LongSettingDataContract(10L)),
            new(nameof(settings.SecretSetting), new StringSettingDataContract("very secret password")),
            new(nameof(settings.StringCollectionSetting), new DataGridSettingDataContract(new List<Dictionary<string, object?>>
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
            new(nameof(settings.ObjectListSetting), new DataGridSettingDataContract(new List<Dictionary<string, object?>>
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
            })),
            new(nameof(settings.JsonSetting), new JsonSettingDataContract("""
                                                                          {
                                                                            "Key": "sampleKey",
                                                                            "Value": "sampleValue",
                                                                            "MyInt": 42
                                                                          }
                                                                          """)),
            new(nameof(settings.EnumSetting), new StringSettingDataContract("Dog")),
            new(nameof(settings.EnvironmentSpecificSetting), new StringSettingDataContract("EnvSpecific"))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);

        Assert.That(updatedSettings.Count, Is.EqualTo(14));
        foreach (var setting in updatedSettings)
        {
            var originalSetting = settingsToUpdate.First(a => a.Name == setting.Name);
            Assert.That(setting.Value?.GetType(), Is.EqualTo(originalSetting.Value?.GetType()),
                $"Setting {setting.Name} should have the same type");
            if (originalSetting.GetType().IsSupportedBaseType())
                AssertJsonEquivalence(setting.Value, originalSetting.Value);
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
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret);

        var oldValue = "Horse";
        const string newValue = "A new value";
        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AStringSetting), new StringSettingDataContract(newValue))
        };
    
        const string instanceName = "Instance1";
        await SetSettings(settings.CurrentValue.ClientName, updatedSettings, instanceName);
    
        var noInstanceSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret);
        var instanceSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret, instanceName);

        Assert.That(noInstanceSettings.First(a => a.Name == nameof(settings.CurrentValue.AStringSetting)).Value?.GetValue(), Is.EqualTo(oldValue));
        Assert.That(instanceSettings.First(a => a.Name == nameof(settings.CurrentValue.AStringSetting)).Value?.GetValue(), Is.EqualTo(newValue));
        Assert.That(noInstanceSettings.First(a => a.Name == nameof(settings.CurrentValue.AnIntSetting)).Value?.GetValue(), 
            Is.EqualTo(instanceSettings.First(a => a.Name == nameof(settings.CurrentValue.AnIntSetting)).Value?.GetValue()));
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
    
        var instance1Settings = await GetSettingsForClient(settings.ClientName, secret, instance1Name);
    
        var instance2Settings = await GetSettingsForClient(settings.ClientName, secret, instance2Name);

        Assert.That(noInstanceSettings.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue(), Is.EqualTo(oldValue));
        Assert.That(instance1Settings.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue(), Is.EqualTo(newValue1));
        Assert.That(instance2Settings.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue(), Is.EqualTo(newValue2));
    }

    [Test]
    public async Task ShallReturnErrorWhenSettingSettingsForNonMatchingClient()
    {
        await RegisterSettings<ThreeSettings>();

        var updatedSettings = new List<SettingDataContract>
        {
            new("Some setting", new StringSettingDataContract("some new value"))
        };

        var contract = new SettingValueUpdatesDataContract(updatedSettings, "testing");

        var requestUri = $"/clients/{Uri.EscapeDataString("someUnknownClient")}/settings";

        await ApiClient.PutAndVerify(requestUri, contract, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallReturnErrorWhenSettingInvalidValueForSetting()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AnIntSetting), new StringSettingDataContract("This is a string"))
        };
        
        var contract = new SettingValueUpdatesDataContract(settingsToUpdate, string.Empty);

        var requestUri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/settings";

        await ApiClient.PutAndVerify(requestUri, contract, HttpStatusCode.BadRequest);
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
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.NoSecret), new StringSettingDataContract(noSecretValue)),
            new(nameof(settings.SecretNoDefault), new StringSettingDataContract(secret1Value)),
            new(nameof(settings.SecretWithDefault), new StringSettingDataContract(secret2Value)),
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


        var settingValues = await GetSettingsForClient(settings.ClientName, secret);
        Assert.That(GetSettingValue(settingValues, nameof(settings.NoSecret)), Is.EqualTo(noSecretValue));
        Assert.That(GetSettingValue(settingValues, nameof(settings.SecretNoDefault)), Is.EqualTo(secret1Value));
        Assert.That(GetSettingValue(settingValues, nameof(settings.SecretWithDefault)), Is.EqualTo(secret2Value));

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

        var jsonValue = """
                        {
                          "Key": "sampleKey",
                          "Value": "sampleValue",
                          "MyInt": 42
                        }
                        """;

        var clients = await GetAllClients();

        var jsonSchema = clients.Single()
            .Settings.FirstOrDefault(a => a.Name == nameof(AllSettingsAndTypes.JsonSetting))?
            .JsonSchema;
        Assert.That(jsonSchema, Is.Not.Null);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.JsonSetting), new StringSettingDataContract(jsonValue))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var settingValues = await GetSettingsForClient(settings.ClientName, secret);

        var kvpSetting = settingValues.FirstOrDefault(a => a.Name == nameof(settings.JsonSetting));
        Assert.That(kvpSetting?.Value?.GetValue(), Is.EqualTo(jsonValue));
    }
    
    [Test]
    public async Task ShallSetLastChangedTimeForValue()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetClient(settings.ClientName);

        var updatedSetting = updatedSettings.Settings.First(a => a.Name == nameof(settings.AStringSetting));
        var notUpdatedSettings = updatedSettings.Settings.Where(a => a.Name != nameof(settings.AStringSetting));
        
        Assert.That(updatedSetting.LastChanged, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        foreach (var setting in notUpdatedSettings)
            Assert.That(setting.LastChanged, Is.Null);
    }
    
    [Test]
    [Retry(3)]
    public async Task ShallNotClearLastChangedTimeOnSettingsRegistration()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ClientXWithTwoSettings>(secret);
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.SingleStringSetting), new StringSettingDataContract(newValue))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);
        
        await RegisterSettings<ClientXWithThreeSettings>(secret);

        var updatedSettings = await GetClient(settings.ClientName);

        var updatedSetting = updatedSettings.Settings.First(a => a.Name == nameof(settings.SingleStringSetting));
        var notUpdatedSettings = updatedSettings.Settings.Where(a => a.Name != nameof(settings.SingleStringSetting));
        
        Assert.That(updatedSetting.LastChanged, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
        foreach (var setting in notUpdatedSettings)
            Assert.That(setting.LastChanged, Is.Null);
    }

    [Test]
    public async Task ShallReturnUnauthorizedWhenTryingToUpdateSettingsForClientNotMatchingUserFilter()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };
        
        var user = NewUser(role: Role.User, clientFilter: $"someNotMatchingFilter");
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);

        var result = await SetSettings(settings.ClientName, settingsToUpdate, tokenOverride: loginResult.Token, validateSuccess: false);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    [Retry(3)]
    public async Task ShallAutomaticallyUpdateSettingsWhenChanged()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 500));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ThreeSettings>(secret);
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AStringSetting), new StringSettingDataContract(newValue))
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);

        await WaitForCondition(() => Task.FromResult(settings.CurrentValue.AStringSetting == newValue), TimeSpan.FromSeconds(5));

        Assert.That(settings.CurrentValue.AStringSetting, Is.EqualTo(newValue));
    }
    
    [Test]
    public async Task ShallSetMultiSelectListValueInDataGrid()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 500));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ClientWithCollections>(secret);

        var newValue = new List<Dictionary<string, object?>>()
        {
            new()
            {
                { nameof(AnimalDetail.Category), "Land" },
                { nameof(AnimalDetail.FavouriteFoods), new List<string>() { "Hay", "Corn" } },
                { nameof(AnimalDetail.HeightCm), 50 },
                { nameof(AnimalDetail.Name), "Sally" },
            },
            new()
            {
                { nameof(AnimalDetail.Category), "Land" },
                { nameof(AnimalDetail.FavouriteFoods), new List<string>() },
                { nameof(AnimalDetail.HeightCm), 60 },
                { nameof(AnimalDetail.Name), "Syd" },
            },
            new()
            {
                { nameof(AnimalDetail.Category), "Sea" },
                { nameof(AnimalDetail.FavouriteFoods), new List<string>() { "Meat" } },
                { nameof(AnimalDetail.HeightCm), 70 },
                { nameof(AnimalDetail.Name), "Sam" },
            },
        };

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.AnimalDetails), new DataGridSettingDataContract(newValue))
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);

        await WaitForCondition(() => Task.FromResult(settings.CurrentValue.AnimalDetails?.Count == 3), TimeSpan.FromSeconds(5));

        var json = JsonConvert.SerializeObject(settings.CurrentValue.AnimalDetails);

        var expected = """
                       [
                         {
                           "Name": "Sally",
                           "Category": "Land",
                           "HeightCm": 50,
                           "FavouriteFoods": [
                             "Hay",
                             "Corn"
                           ]
                         },
                         {
                           "Name": "Syd",
                           "Category": "Land",
                           "HeightCm": 60,
                           "FavouriteFoods": null
                         },
                         {
                           "Name": "Sam",
                           "Category": "Sea",
                           "HeightCm": 70,
                           "FavouriteFoods": [
                             "Meat"
                           ]
                         }
                       ]
                       """;

        string normalizedResponse = Regex.Replace(json, @"\s", "");
        string normalizedExpected = Regex.Replace(expected, @"\s", "");
        Assert.That(normalizedResponse, Is.EqualTo(normalizedExpected));
    }

    [Test]
    public async Task ShallHandleEnumSettings()
    {
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 500));
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<EnumSettings>(secret);

        var clients = await GetAllClients();

        var webSettings1 = clients!.Single().Settings;
        Assert.That(webSettings1.First(a => a.Name == nameof(EnumSettings.PetWithDefault)).Value?.GetValue(),
            Is.EqualTo(Pets.Fish.ToString()));
        
        Assert.That(webSettings1.First(a => a.Name == nameof(EnumSettings.OptionalPetWithDefault)).Value?.GetValue(),
            Is.EqualTo(Pets.Cat.ToString()));
        
        Assert.That(JsonConvert.SerializeObject(webSettings1.First(a => a.Name == nameof(EnumSettings.PetGroupsWithDefault)).Value?.GetValue()),
            Is.EqualTo(JsonConvert.SerializeObject(EnumSettings.GetDefaultPetGroups(), new StringEnumConverter())));

        var newValue = new List<Dictionary<string, object?>>()
        {
            new()
            {
                { nameof(PetGroup.Name), "Spot" },
                { nameof(PetGroup.PetInGroup), Pets.Dog.ToString() },
                { nameof(PetGroup.OptionalPetInGroup), Pets.Cat.ToString() }
            },
            new()
            {
                { nameof(PetGroup.Name), "Bubbles" },
                { nameof(PetGroup.PetInGroup), Pets.Fish.ToString() },
                { nameof(PetGroup.OptionalPetInGroup), Constants.EnumNullPlaceholder }
            }
        };

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.PetGroups), new DataGridSettingDataContract(newValue)),
            new(nameof(settings.CurrentValue.Pet), new StringSettingDataContract(Pets.Dog.ToString())),
            new(nameof(settings.CurrentValue.OptionalPet), new StringSettingDataContract(null)),
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);

        await WaitForCondition(() => Task.FromResult(settings.CurrentValue.PetGroups?.Count == 2), TimeSpan.FromSeconds(5));

        var json = JsonConvert.SerializeObject(settings.CurrentValue.PetGroups);

        var expected = """
                       [ {
                         "Name" : "Spot",
                         "PetInGroup" : 1,
                         "OptionalPetInGroup" : 0
                       }, {
                         "Name" : "Bubbles",
                         "PetInGroup" : 2,
                         "OptionalPetInGroup" : null
                       } ]
                       """;

        string normalizedResponse = Regex.Replace(json, @"\s", "");
        string normalizedExpected = Regex.Replace(expected, @"\s", "");
        Assert.That(normalizedResponse, Is.EqualTo(normalizedExpected));
    }
    
    [Test]
    [TestCase(Classification.Functional)]
    [TestCase(Classification.Technical)]
    [TestCase(Classification.Special)]
    [TestCase(Classification.Functional, Classification.Technical)]
    [TestCase(Classification.Special, Classification.Functional)]
    [TestCase(Classification.Special, Classification.Technical)]
    [TestCase(Classification.Special, Classification.Technical, Classification.Functional)]
    public async Task ShallOnlyBePresentedSettingsWithMatchingClassifications(params Classification[] classifications)
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClassifiedSettings>(secret);
        var user = NewUser(role: Role.User, allowedClassifications: classifications.ToList());
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);

        var clients = await GetAllClients(tokenOverride: loginResult.Token);
        var client = clients.Single();

        Assert.That(client.Settings.Count, Is.EqualTo(classifications.Length));
        foreach (var setting in client.Settings)
        {
            Assert.That(setting.Classification, Is.AnyOf(classifications));
        }
    }
    
    [Test]
    public async Task ShallOnlyPresentClientsThatContainMatchingClassificationsOfSettings()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClassifiedSettings>(secret);
        await RegisterSettings<ClientA>(secret);

        var user = NewUser(role: Role.User, allowedClassifications: [Classification.Functional]);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);

        var clients = await GetAllClients(tokenOverride: loginResult.Token);
        
        var client = clients.Single();

        Assert.That(client.Settings.Count, Is.EqualTo(1));
    }
    
    [Test]
    public async Task ShallPreventUsersFromUpdatingSettingsWithClassificationsTheyDoNotHave()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ClassifiedSettings>(secret);
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(ClassifiedSettings.SpecialSetting), new StringSettingDataContract(newValue))
        };

        var user = NewUser(role: Role.User, allowedClassifications: [Classification.Functional]);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        var result = await SetSettings(settings.ClientName, settingsToUpdate, tokenOverride: loginResult.Token, validateSuccess: false);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
    
    [Test]
    public async Task ShallAllowUsersToUpdateSettingsMatchingTheirClassification()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ClassifiedSettings>(secret);
        const string newValue = "Some new value";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(ClassifiedSettings.FunctionalSetting), new StringSettingDataContract(newValue))
        };

        var user = NewUser(role: Role.User, allowedClassifications: [Classification.Functional]);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        var result = await SetSettings(settings.ClientName, settingsToUpdate, tokenOverride: loginResult.Token);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ShallUpdateAndApplySettingsWithConfigurationSectionOverrides()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(pollIntervalOverrideMs: 500));
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<ConfigSectionOverrideSettings>(secret);

        // List of settings to update
        var settingsToUpdate = new List<SettingDataContract>
        {
            // Update simple setting with single override
            new(nameof(settings.CurrentValue.BasicSetting), new StringSettingDataContract("UpdatedValue")),
            
            // Update setting with multiple overrides
            new(nameof(settings.CurrentValue.MultiSectionSetting), new StringSettingDataContract("UpdatedAppName")),
            
            // Update int setting
            new(nameof(settings.CurrentValue.MaxConnections), new IntSettingDataContract(200)),
            
            // Update nested setting with multiple overrides
            new($"{nameof(settings.CurrentValue.Database)}->{nameof(settings.CurrentValue.Database.Provider)}", 
                new StringSettingDataContract("PostgreSQL")),
            
            // Update JSON setting
            new(nameof(settings.CurrentValue.ApplicationConfig), new JsonSettingDataContract(
                """
                {
                  "AppName": "UpdatedAppName",
                  "AppVersion": 2
                }
                """))
        };

        // Act - Update the settings
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);

        // Wait for settings to be updated automatically
        await WaitForCondition(() => 
            Task.FromResult(settings.CurrentValue is { BasicSetting: "UpdatedValue", MultiSectionSetting: "UpdatedAppName" }), 
            TimeSpan.FromSeconds(5));

        // Assert - Verify the settings were updated in the provider
        
        // Check original settings
        Assert.That(settings.CurrentValue.BasicSetting, Is.EqualTo("UpdatedValue"));
        Assert.That(settings.CurrentValue.MultiSectionSetting, Is.EqualTo("UpdatedAppName"));
        Assert.That(settings.CurrentValue.MaxConnections, Is.EqualTo(200));
        Assert.That(settings.CurrentValue.Database?.Provider, Is.EqualTo("PostgreSQL"));
        Assert.That(settings.CurrentValue.ApplicationConfig?.AppName, Is.EqualTo("UpdatedAppName"));
        Assert.That(settings.CurrentValue.ApplicationConfig?.AppVersion, Is.EqualTo(2));
        
        // Check overridden settings in configuration sections
        Assert.That(configuration["AppSettings:BasicSetting"], Is.EqualTo("UpdatedValue"));
        Assert.That(configuration["AppSettings:ApplicationName"], Is.EqualTo("UpdatedAppName"));
        Assert.That(configuration["AppSettings:MaxConnections"], Is.EqualTo("200"));
        
        // Check multi-section overrides
        Assert.That(configuration["Configuration:AppName"], Is.EqualTo("UpdatedAppName"));
        
        // Check nested settings with multiple overrides
        Assert.That(configuration["Database:Provider"], Is.EqualTo("PostgreSQL"));
        Assert.That(configuration["ConnectionStrings:ProviderName"], Is.EqualTo("PostgreSQL"));
        
        // Check JSON settings with overrides
        Assert.That(configuration["Application:Config:AppName"], Is.EqualTo("UpdatedAppName"));
        Assert.That(configuration["Application:Config:AppVersion"], Is.EqualTo("2"));
    }
}