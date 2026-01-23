using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Authentication;
using Fig.Contracts.SettingClients;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NJsonSchema;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class SettingsRegistrationTests : IntegrationTestBase
{
    [Test]
    public async Task ShallRegisterSingleClient()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(clients.First().Name, Is.EqualTo(settings.ClientName));
        Assert.That(clients.First().Settings.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallRegisterMultipleClients()
    {
        await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientXWithTwoSettings>();
        await RegisterSettings<AllSettingsAndTypes>();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(3));

        var clientNames = string.Join(",", clients.Select(a => a.Name).OrderBy(a => a));
        Assert.That(clientNames, Is.EqualTo("AllSettingsAndTypes,ClientX,ThreeSettings"));
    }

    [Test]
    public async Task ShallUpdateSettingsDefinitionToAddSettings()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientXWithTwoSettings>(secret);
        await RegisterSettings<ClientXWithThreeSettings>(secret);

        var clients = (await GetAllClients()).ToList();

        const string expectedResult =
            "DateOfBirth:The date of birth:," +
            "IsCool:True if cool:True," +
            "SingleStringSetting:This is a single string updated:Pig";
        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(clients.First().Settings.Count, Is.EqualTo(3));
        var settingDetails =
            string.Join(",", clients.First().Settings
                .OrderBy(a => a.Name)
                .Select(a => $"{a.Name}:{a.Description}:{a.Value?.GetValue()}"));
        Assert.That(settingDetails, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task ShallUpdateSettingsDefinitionToRemoveSettings()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientXWithThreeSettings>(secret);
        await RegisterSettings<ClientXWithTwoSettings>(secret);

        var clients = (await GetAllClients()).ToList();

        const string expectedResult =
            "FavouriteNumber:This is an int default 4:4," +
            "SingleStringSetting:This is a single string:Pig";
        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(clients.First().Settings.Count, Is.EqualTo(2));
        var settingDetails =
            string.Join(",", clients.First().Settings
                .OrderBy(a => a.Name)
                .Select(a => $"{a.Name}:{a.Description}:{a.Value?.GetValue()}"));
        Assert.That(settingDetails, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task SecondRegistrationShouldNotUpdateValues()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        const string updatedString = "Some new value";
        const int updatedInt = 99;

        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(updatedString)),
            new(nameof(settings.AnIntSetting), new IntSettingDataContract(updatedInt))
        };

        await SetSettings(settings.ClientName, updatedSettings);
        await RegisterSettings<ThreeSettings>(secret);

        var finalSettings = (await GetSettingsForClient(settings.ClientName, secret)).ToList();

        Assert.That(finalSettings.Count, Is.EqualTo(3));
        Assert.That(finalSettings.First(a => a.Name == nameof(settings.AStringSetting)).Value?.GetValue(),
            Is.EqualTo(updatedString));
        Assert.That(finalSettings.First(a => a.Name == nameof(settings.AnIntSetting)).Value?.GetValue(),
            Is.EqualTo(updatedInt));
        Assert.That(finalSettings.First(a => a.Name == nameof(settings.ABoolSetting)).Value?.GetValue(),
            Is.True);
    }

    [Test]
    public async Task RegistrationShouldUpdateAllInstances()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ClientXWithThreeSettings>(secret);

        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.SingleStringSetting), new StringSettingDataContract("some new value"))
        };

        await SetSettings(settings.ClientName, updatedSettings, "Instance1");

        var clientsBeforeRegistration = (await GetAllClients()).ToList();
        Assert.That(clientsBeforeRegistration.Count, Is.EqualTo(2));
        Assert.That(clientsBeforeRegistration.All(c => c.Settings.Count() == 3));

        await RegisterSettings<ClientXWithTwoSettings>(secret);

        var clientsAfterRegistration = (await GetAllClients()).ToList();

        Assert.That(clientsAfterRegistration.Count, Is.EqualTo(2));
        Assert.That(clientsAfterRegistration.All(c => c.Settings.Count() == 2));
    }

    [TestCase(null, false)]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("tooshort")]
    public async Task ShallNotAcceptRegistrationWithoutValidClientSecret(string clientSecret, bool provideSecret = true)
    {
        var settings = new ThreeSettings();
        var dataContract = settings.CreateDataContract(settings.ClientName);
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        if (provideSecret)
            httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);

        var result = await httpClient.PostAsync("/clients", data);
        var error = await GetErrorResult(result);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
            $"Expected unauthorized but was: {error}");
    }

    [Test]
    public async Task ShallReturnUnauthorizedForSecondRegistrationWithDifferentSecret()
    {
        var settings = new ThreeSettings();
        var dataContract = settings.CreateDataContract(settings.ClientName);
        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", Guid.NewGuid().ToString());
        var result = await httpClient.PostAsync("/clients", data);

        Assert.That(result.IsSuccessStatusCode, Is.True);

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("clientSecret", Guid.NewGuid().ToString());
        var result2 = await httpClient.PostAsync("/clients", data);

        Assert.That(result2.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallAcceptBothCurrentAndPreviousSecretsForRegistrationDuringSecretChangePeriod()
    {
        var originalSecret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(originalSecret);

        var updatedSecret = GetNewSecret();
        await ChangeClientSecret(settings.ClientName, updatedSecret, DateTime.UtcNow.AddMinutes(1));
        
        await RegisterSettings<ThreeSettings>(originalSecret);
        await RegisterSettings<ThreeSettings>(updatedSecret);
    }
    
    [Test]
    public async Task ShallNotAcceptPreviousSecretForRegistrationAfterChangePeriodExpiry()
    {
        var originalSecret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(originalSecret);

        var updatedSecret = GetNewSecret();
        await ChangeClientSecret(settings.ClientName, updatedSecret, DateTime.UtcNow);

        var dataContract = settings.CreateDataContract(settings.ClientName);
        var json = JsonConvert.SerializeObject(dataContract, JsonSettings.FigDefault);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        const string requestUri = "/clients";
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("clientSecret", originalSecret);
        var result = await httpClient.PostAsync(requestUri, content);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallAcceptBothCurrentAndPreviousSecretsForSettingRetrievalDuringSecretChangePeriod()
    {
        var originalSecret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(originalSecret);

        var updatedSecret = GetNewSecret();
        await ChangeClientSecret(settings.ClientName, updatedSecret, DateTime.UtcNow.AddMinutes(1));

        var settings1 = await GetSettingsForClient(settings.ClientName, originalSecret);
        Assert.That(settings1.Count, Is.EqualTo(3));
        
        var settings2 = await GetSettingsForClient(settings.ClientName, updatedSecret);
        Assert.That(settings2.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallNotAcceptPreviousSecretForSettingRetrievalAfterChangePeriodExpiry()
    {
        var originalSecret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(originalSecret);

        var updatedSecret = GetNewSecret();
        await ChangeClientSecret(settings.ClientName, updatedSecret, DateTime.UtcNow);

        var requestUri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/settings";
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", originalSecret);
        var result = await httpClient.GetAsync(requestUri);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallNotAllowSecretChangesForNonAdministrators()
    {
        var originalSecret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(originalSecret);

        var user = NewUser();
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        var updatedSecret = GetNewSecret();
        var request = new ClientSecretChangeRequestDataContract(updatedSecret, DateTime.UtcNow);

        var uri = $"clients/{Uri.EscapeDataString(settings.ClientName)}/secret";
        var json = JsonConvert.SerializeObject(request, JsonSettings.FigDefault);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", loginResult.Token);

        var result = await httpClient.PutAsync(uri, content);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallOnlyReturnClientsThatMatchUserFilter()
    {
        var settings1 = await RegisterSettings<ThreeSettings>();
        var settings2 = await RegisterSettings<ClientXWithTwoSettings>();
        await RegisterSettings<AllSettingsAndTypes>();

        var user = NewUser(role: Role.Administrator, clientFilter: $"{settings1.ClientName}|{settings2.ClientName}");
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        var clients = (await GetAllClients(tokenOverride: loginResult.Token)).ToList();

        Assert.That(clients.Count, Is.EqualTo(2));

        var clientNames = string.Join(",", clients.Select(a => a.Name).OrderBy(a => a));
        Assert.That(clientNames, Is.EqualTo($"{settings2.ClientName},{settings1.ClientName}"));
    }

    [Test]
    public async Task ShallNotAllowRegistrationsWithInvalidClientNames()
    {
        var settings = Activator.CreateInstance<InvalidSettings>();
        var dataContract = settings.CreateDataContract(settings.ClientName);

        const string requestUri = "/clients";
        var clientSecret = GetNewSecret();
        var result = await ApiClient.Post(requestUri, dataContract, clientSecret, validateSuccess: false);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var error = await GetErrorResult(result);
        Assert.That(error!.Message.Contains($"'{settings.ClientName}' is not a valid name"), error.Message);
    }

    [Test]
    public async Task ShallRegisterWithSettingOverrides()
    {
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));

        var settingOverrides = CreateOverrides();

        var settings = await RegisterSettings<ThreeSettings>(settingOverrides: settingOverrides);

        var clients = await GetAllClients();
        var match = clients.FirstOrDefault(a => a.Name == settings.ClientName);

        foreach (var settingOverride in settingOverrides)
        {
            var updatedSetting = match?.Settings.FirstOrDefault(a => a.Name == settingOverride.Name);
            Assert.That(updatedSetting?.Value?.GetValue(), Is.EqualTo(settingOverride.Value?.GetValue()));
        }
    }
    
    [Test]
    public async Task ShallNotAllowSettingOverridesIfDisabled()
    {
        await SetConfiguration(CreateConfiguration(allowClientOverrides: false, clientOverrideRegex: ".*"));

        var settingOverrides = CreateOverrides();

        var settings = await RegisterSettings<ThreeSettings>(settingOverrides: settingOverrides);

        var clients = await GetAllClients();
        var match = clients.FirstOrDefault(a => a.Name == settings.ClientName);

        foreach (var settingOverride in settingOverrides)
        {
            var updatedSetting = match?.Settings.FirstOrDefault(a => a.Name == settingOverride.Name);
            Assert.That(updatedSetting?.Value?.GetValue(), Is.Not.EqualTo(settingOverride.Value?.GetValue()));
        }
    }
    
    [Test]
    public async Task ShallNotAllowSettingOverridesIfClientNameDoesNotMatchRegex()
    {
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: "someNonMatch"));

        var settingOverrides = CreateOverrides();

        var settings = await RegisterSettings<ThreeSettings>(settingOverrides: settingOverrides);

        var clients = await GetAllClients();
        var match = clients.FirstOrDefault(a => a.Name == settings.ClientName);

        foreach (var settingOverride in settingOverrides)
        {
            var updatedSetting = match?.Settings.FirstOrDefault(a => a.Name == settingOverride.Name);
            Assert.That(updatedSetting?.Value?.GetValue(), Is.Not.EqualTo(settingOverride.Value?.GetValue()));
        }
    }
    
    [Test]
    public async Task ShallOnlyRegisterClientOnce()
    {
        DisableTransactionMiddleware();
        DisableTimeMachineWorker();
        
        var tasks = new Task[5];
        var secret = GetNewSecret();
        for (var i = 0; i < 5; i++)
        {
            tasks[i] = RegisterSettings<ThreeSettings>(secret);
        }

        await Task.WhenAll(tasks);

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
    }
    
    [TestCase("en-US")] // English (United States)
    [TestCase("fr-FR")] // French (France)
    [TestCase("de-DE")] // German (Germany)
    [TestCase("ja-JP")] // Japanese (Japan)
    [TestCase("ar-SA")] // Arabic (Saudi Arabia)
    public void ShallHandleDifferentCulturesForDefaultValues(string cultureName)
    {
        // Store the original culture so it can be restored later
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            // Set the thread culture and UI culture to the test culture
            var testCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = testCulture;
            CultureInfo.CurrentUICulture = testCulture;

            // Execute the test code
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<ClientWithCultureBasedSettings>(secret);

            var defaultClient = new ClientWithCultureBasedSettings
            {
                Items = ClientWithCultureBasedSettings.GetItems()
            };
            Assert.That(settings.CurrentValue.NormalDouble, Is.EqualTo(defaultClient.NormalDouble));
            Assert.That(settings.CurrentValue.NullableDouble, Is.EqualTo(defaultClient.NullableDouble));
            Assert.That(settings.CurrentValue.DateTime, Is.EqualTo(defaultClient.DateTime));
            Assert.That(settings.CurrentValue.Timespan, Is.EqualTo(defaultClient.Timespan));
            Assert.That(settings.CurrentValue.Items.Select(x => new { x.Height, x.Weight }),
                Is.EqualTo(ClientWithCultureBasedSettings.GetItems().Select(x => new { x.Height, x.Weight })));
        }
        finally
        {
            // Restore the original culture to avoid affecting other tests
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
    
    [TestCase("en-US")] // English (United States)
    [TestCase("fr-FR")] // French (France)
    [TestCase("de-DE")] // German (Germany)
    [TestCase("ja-JP")] // Japanese (Japan)
    [TestCase("ar-SA")] // Arabic (Saudi Arabia)
    public void ShallHandleDifferentCultures(string cultureName)
    {
        // Store the original culture so it can be restored later
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            // Set the thread culture and UI culture to the test culture
            var testCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = testCulture;
            CultureInfo.CurrentUICulture = testCulture;

            // Execute the test code
            var secret = GetNewSecret();
            var (settings, configuration) = InitializeConfigurationProvider<ClientWithCultureBasedSettings>(secret);

            configuration.Reload();
            
            var defaultClient = new ClientWithCultureBasedSettings
            {
                Items = ClientWithCultureBasedSettings.GetItems()
            };
            Assert.That(settings.CurrentValue.NormalDouble, Is.EqualTo(defaultClient.NormalDouble));
            Assert.That(settings.CurrentValue.NullableDouble, Is.EqualTo(defaultClient.NullableDouble));
            Assert.That(settings.CurrentValue.DateTime, Is.EqualTo(defaultClient.DateTime));
            Assert.That(settings.CurrentValue.Timespan, Is.EqualTo(defaultClient.Timespan));
            Assert.That(settings.CurrentValue.Items.Select(x => new { x.Height, x.Weight }),
                Is.EqualTo(ClientWithCultureBasedSettings.GetItems().Select(x => new { x.Height, x.Weight })));
        }
        finally
        {
            // Restore the original culture to avoid affecting other tests
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public async Task ShallSupportJsonSettings()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<SettingsWithJson>(secret);

        var clients = await GetAllClients();

        var petSetting = clients.Single().Settings.Single();
        Assert.That(petSetting.JsonSchema, Is.Not.Null);
        
        var schema = JsonSchema.FromJsonAsync(petSetting.JsonSchema!).Result;
        var sampleValue = schema.ToSampleJson().ToString();

        await SetSettings(settings.CurrentValue.ClientName, new List<SettingDataContract>
        {
            new(petSetting.Name, new StringSettingDataContract(sampleValue))
        });
        
        configuration.Reload();
        
        Assert.That(settings.CurrentValue.Pet?.Name, Is.EqualTo("Name"));
    }

    [Test]
    public async Task ShallRegisterNestedSettings()
    {
        const string updatedSchoolName = "Updated School";
        const string updatedSubjectName = "Updated Subject";
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<SettingsWithNesting>(secret);

        var clients = await GetAllClients();

        var studentsSetting = clients.Single().Settings.FirstOrDefault(a => a.Name == "School->Students");
        Assert.That(studentsSetting?.JsonSchema, Is.Not.Null);
        
        var json = """
                   [ {
                     "Name" : "Jim",
                     "Subjects" : [ {
                       "Name" : "Math",
                       "Grade" : 55
                     } ]
                   } ]
                   """;
        
        var updatedSettings = new List<SettingDataContract>
        {
            new("School->Name", new StringSettingDataContract(updatedSchoolName)),
            new("School->Subjects", new DataGridSettingDataContract([
                new()
                {
                    { "Name", updatedSubjectName }, { "Grade", 100 }
                },

                new()
                {
                    { "Name", "frank" }, { "Grade", 50 }
                }
            ])),
            new("School->Students", new StringSettingDataContract(json))
        };

        await SetSettings(settings.CurrentValue.ClientName, updatedSettings);
        
        configuration.Reload();

        Assert.That(settings.CurrentValue.School!.Name, Is.EqualTo(updatedSchoolName));
        Assert.That(settings.CurrentValue.School!.Subjects![0].Name, Is.EqualTo(updatedSubjectName));
        Assert.That(settings.CurrentValue.School!.Students![0].Name, Is.EqualTo("Jim"));
        Assert.That(settings.CurrentValue.School!.Students![0].Subjects![0].Name, Is.EqualTo("Math"));
        Assert.That(settings.CurrentValue.School.Students![0].Subjects![0].Grade, Is.EqualTo(55));
        Assert.That(settings.CurrentValue.Subject, Is.Null);
    }

    [Test]
    public async Task ShallRegisterSequentialSettingsWithSameNameAndDifferentTypes()
    {
        // Arrange
        var secret = GetNewSecret();
        
        // Step 1: Register ClientBString (string-based setting)
        var stringClient = await RegisterSettings<ClientBString>(secret);
        
        // Verify string setting is registered correctly
        var stringClientSettings = await GetSettingsForClient(stringClient.ClientName, secret);
        
        Assert.That(stringClientSettings[0], Is.Not.Null, "ClientB should be registered successfully");
        Assert.That(stringClientSettings[0].Value?.GetValue(), Is.EqualTo("Dog, Cat, Bird"));

        // Step 2: Register ClientBList (list of Animals)
        var listClient = await RegisterSettings<ClientBList>(secret);
        
        // Verify list setting is registered correctly
        var listClientSetting = await GetSettingsForClient(listClient.ClientName, secret);
        
        Assert.That(listClientSetting[0], Is.Not.Null, "ClientB should be registered successfully");
        var value = listClientSetting[0].Value?.GetValue() as List<Dictionary<string, object?>> ?? [];
        Assert.That(value.Count , Is.EqualTo(2));
        Assert.That(value[0]["Name"], Is.EqualTo("Name0"));
        Assert.That(value[0]["Legs"], Is.EqualTo(0));
        Assert.That(value[1]["Name"], Is.EqualTo("Name1"));
        Assert.That(value[1]["Legs"], Is.EqualTo(1));
    }

    [Test]
    public async Task ShallSerializeConcurrentRegistrationsForSameClient()
    {
        // Arrange
        var secret = GetNewSecret();
        var registrationTasks = new Task[3];

        // Act - Attempt to register the same client multiple times concurrently
        for (var i = 0; i < registrationTasks.Length; i++)
        {
            registrationTasks[i] = RegisterSettings<ThreeSettings>(secret);
        }

        await Task.WhenAll(registrationTasks);

        // Assert
        var clients = (await GetAllClients()).ToList();
        Assert.That(clients.Count, Is.EqualTo(1), "Only one client should be registered despite concurrent attempts");
        Assert.That(clients.First().Name, Is.EqualTo(nameof(ThreeSettings)));
    }

    [Test]
    public async Task ShallHandleMultipleClientsOfSameTypeRegistering()
    {
        // Arrange
        var secret1 = GetNewSecret();
        
        var tasks = new Task[2];

        // Act - Mix of same client (ThreeSettings) and different client (ClientX) registrations
        tasks[0] = RegisterSettings<ThreeSettings>(secret1);
        tasks[1] = RegisterSettings<ThreeSettings>(secret1);

        await Task.WhenAll(tasks);

        // Assert
        var clients = (await GetAllClients()).ToList();
        Assert.That(clients.Count, Is.EqualTo(1), "Should have exactly 1 unique client registered");
        
        var clientNames = clients.Select(c => c.Name);
        Assert.That(clientNames, Does.Contain(nameof(ThreeSettings)));
    }

    [Test]
    public async Task ShallHandleConcurrentRegistrationWithInstanceOverrides()
    {
        // Arrange
        var secret = GetNewSecret();

        // Act - Register default instance and multiple instance overrides concurrently
        var task1 = RegisterSettings<ThreeSettings>(secret, instance: "Instance1");
        var task2 = RegisterSettings<ThreeSettings>(secret, instance: "Instance2");

        await Task.WhenAll(task1, task2);

        // Assert
        var clients = (await GetAllClients()).ToList();
        
        var threeSettingsClients = clients.Where(c => c.Name == nameof(ThreeSettings)).ToList();
        Assert.That(threeSettingsClients.Count, Is.EqualTo(1), 
            "Should have just the default instance");
    }
    
     [Test]
    public async Task ShallHandleConcurrentInstanceRegistrationsWithoutNullSettingValues()
    {
        // Arrange
        var secret = GetNewSecret();
        
        // Register the client first (without instance)
        var settings = await RegisterSettings<ThreeSettings>(secret);

        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("Value for instance one"))
        }, "one");
        
        await SetSettings(settings.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("Value for instance two"))
        }, "two");

        // Assert - Verify all clients are registered correctly
        var allClients = (await GetAllClients()).ToList();
        Assert.That(allClients.Count, Is.EqualTo(3), "Should have 3 clients: default, instance one, and instance two");
        
        var defaultClient = allClients.FirstOrDefault(c => c.Name == settings.ClientName && c.Instance == null);
        var instanceOneClient = allClients.FirstOrDefault(c => c.Name == settings.ClientName && c.Instance == "one");
        var instanceTwoClient = allClients.FirstOrDefault(c => c.Name == settings.ClientName && c.Instance == "two");
        
        Assert.That(defaultClient, Is.Not.Null, "Default client should exist");
        Assert.That(instanceOneClient, Is.Not.Null, "Instance 'one' client should exist");
        Assert.That(instanceTwoClient, Is.Not.Null, "Instance 'two' client should exist");

        var task1 = RegisterSettings<ThreeSettings>(secret, "one");
        var task2 = RegisterSettings<ThreeSettings>(secret, "two");
        
        var clientStatus1 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        var task3 = GetStatus("ThreeSettings", secret, clientStatus1, "one");
        
        var clientStatus2 = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);
        var task4 = GetStatus("ThreeSettings", secret, clientStatus2, "two");
        
        await Task.WhenAll(task1, task2, task3, task4);
        
        // Verify settings can be retrieved for each instance
        var settingsOne = await GetSettingsForClient(settings.ClientName, secret, "one");
        var settingsTwo = await GetSettingsForClient(settings.ClientName, secret, "two");
        
        Assert.That(settingsOne.Count, Is.EqualTo(3), "Instance 'one' should return 3 settings");
        Assert.That(settingsTwo.Count, Is.EqualTo(3), "Instance 'two' should return 3 settings");
        
        var stringSettingOne = settingsOne.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting));
        Assert.That(stringSettingOne, Is.Not.Null, "Instance 'one' should have AStringSetting");
        Assert.That(stringSettingOne!.Value, Is.Not.Null, "Instance 'one' AStringSetting value should not be null");
        Assert.That(stringSettingOne.Value!.GetValue(), Is.EqualTo("Value for instance one"), 
            "Instance 'one' should have the correct string value");

        var stringSettingTwo = settingsTwo.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting));
        Assert.That(stringSettingTwo, Is.Not.Null, "Instance 'two' should have AStringSetting");
        Assert.That(stringSettingTwo!.Value, Is.Not.Null, "Instance 'two' AStringSetting value should not be null");
        Assert.That(stringSettingTwo.Value!.GetValue(), Is.EqualTo("Value for instance two"), 
            "Instance 'two' should have the correct string value");
    }

    #region Environment Variable Override Tests
    
    private readonly List<string> _envVarsToClean = new();

    private void SetEnvironmentVariable(string key, string value)
    {
        Environment.SetEnvironmentVariable(key, value);
        _envVarsToClean.Add(key);
    }

    private void CleanupEnvironmentVariables()
    {
        foreach (var envVar in _envVarsToClean)
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }
        _envVarsToClean.Clear();
    }

    [Test]
    public async Task ShallOverrideStringSetting_ViaEnvironmentVariable()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        const string overriddenValue = "OverriddenStringValue";
        SetEnvironmentVariable("StringSetting", overriddenValue);

        try
        {
            // Act - Initialize the configuration provider which reads env vars and sends overrides to server
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert - Verify the setting was overridden on the server
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null, "Client should be registered");

            var stringSetting = client!.Settings.FirstOrDefault(a => a.Name == "StringSetting");
            Assert.That(stringSetting, Is.Not.Null, "StringSetting should exist");
            Assert.That(stringSetting!.Value?.GetValue(), Is.EqualTo(overriddenValue), 
                "StringSetting should have the overridden value");
            Assert.That(stringSetting.IsExternallyManaged, Is.True, 
                "StringSetting should be marked as externally managed");
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallOverrideIntSetting_ViaEnvironmentVariable()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        const int overriddenValue = 999;
        SetEnvironmentVariable("IntSetting", overriddenValue.ToString());

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var intSetting = client!.Settings.FirstOrDefault(a => a.Name == "IntSetting");
            Assert.That(intSetting, Is.Not.Null, "IntSetting should exist");
            Assert.That(intSetting!.Value?.GetValue(), Is.EqualTo(overriddenValue), 
                "IntSetting should have the overridden value");
            Assert.That(intSetting.IsExternallyManaged, Is.True, 
                "IntSetting should be marked as externally managed");
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallOverrideBoolSetting_ViaEnvironmentVariable()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        SetEnvironmentVariable("BoolSetting", "true");

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var boolSetting = client!.Settings.FirstOrDefault(a => a.Name == "BoolSetting");
            Assert.That(boolSetting, Is.Not.Null, "BoolSetting should exist");
            Assert.That(boolSetting!.Value?.GetValue(), Is.EqualTo(true), 
                "BoolSetting should have the overridden value");
            Assert.That(boolSetting.IsExternallyManaged, Is.True, 
                "BoolSetting should be marked as externally managed");
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallOverrideDoubleSetting_ViaEnvironmentVariable()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        const double overriddenValue = 99.99;
        SetEnvironmentVariable("DoubleSetting", overriddenValue.ToString(CultureInfo.InvariantCulture));

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var doubleSetting = client!.Settings.FirstOrDefault(a => a.Name == "DoubleSetting");
            Assert.That(doubleSetting, Is.Not.Null, "DoubleSetting should exist");
            Assert.That(doubleSetting!.Value?.GetValue(), Is.EqualTo(overriddenValue), 
                "DoubleSetting should have the overridden value");
            Assert.That(doubleSetting.IsExternallyManaged, Is.True, 
                "DoubleSetting should be marked as externally managed");
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallOverrideStringList_ViaEnvironmentVariable()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        // Set list items using the __ index pattern
        SetEnvironmentVariable("StringList__0", "Item1");
        SetEnvironmentVariable("StringList__1", "Item2");
        SetEnvironmentVariable("StringList__2", "Item3");

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var stringListSetting = client!.Settings.FirstOrDefault(a => a.Name == "StringList");
            Assert.That(stringListSetting, Is.Not.Null, "StringList should exist");
            Assert.That(stringListSetting!.IsExternallyManaged, Is.True, 
                "StringList should be marked as externally managed");

            // DataGrid values come back as List<Dictionary<string, object?>>
            var listValue = stringListSetting.Value?.GetValue() as List<Dictionary<string, object?>>;
            Assert.That(listValue, Is.Not.Null, "StringList value should be a list");
            Assert.That(listValue!.Count, Is.EqualTo(3), "StringList should have 3 items");
            
            // For simple lists, values are stored with key "Value"
            Assert.That(listValue[0]["Values"], Is.EqualTo("Item1"));
            Assert.That(listValue[1]["Values"], Is.EqualTo("Item2"));
            Assert.That(listValue[2]["Values"], Is.EqualTo("Item3"));
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallOverrideComplexList_ViaEnvironmentVariable()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        // Set complex list items using the __ index __ property pattern
        SetEnvironmentVariable("ComplexList__0__StringVal", "OverriddenString1");
        SetEnvironmentVariable("ComplexList__0__IntVal", "100");
        SetEnvironmentVariable("ComplexList__1__StringVal", "OverriddenString2");
        SetEnvironmentVariable("ComplexList__1__IntVal", "200");

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var complexListSetting = client!.Settings.FirstOrDefault(a => a.Name == "ComplexList");
            Assert.That(complexListSetting, Is.Not.Null, "ComplexList should exist");
            Assert.That(complexListSetting!.IsExternallyManaged, Is.True, 
                "ComplexList should be marked as externally managed");

            // DataGrid values come back as List<Dictionary<string, object?>>
            var listValue = complexListSetting.Value?.GetValue() as List<Dictionary<string, object?>>;
            Assert.That(listValue, Is.Not.Null, "ComplexList value should be a list");
            Assert.That(listValue!.Count, Is.EqualTo(2), "ComplexList should have 2 items");
            
            Assert.That(listValue[0]["StringVal"], Is.EqualTo("OverriddenString1"));
            Assert.That(listValue[0]["IntVal"], Is.EqualTo(100));
            Assert.That(listValue[1]["StringVal"], Is.EqualTo("OverriddenString2"));
            Assert.That(listValue[1]["IntVal"], Is.EqualTo(200));
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallOverrideMultipleSettings_ViaEnvironmentVariables()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        SetEnvironmentVariable("StringSetting", "MultiOverrideString");
        SetEnvironmentVariable("IntSetting", "777");
        SetEnvironmentVariable("BoolSetting", "true");

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var stringSetting = client!.Settings.FirstOrDefault(a => a.Name == "StringSetting");
            var intSetting = client.Settings.FirstOrDefault(a => a.Name == "IntSetting");
            var boolSetting = client.Settings.FirstOrDefault(a => a.Name == "BoolSetting");
            
            Assert.That(stringSetting!.Value?.GetValue(), Is.EqualTo("MultiOverrideString"));
            Assert.That(stringSetting.IsExternallyManaged, Is.True);
            
            Assert.That(intSetting!.Value?.GetValue(), Is.EqualTo(777));
            Assert.That(intSetting.IsExternallyManaged, Is.True);
            
            Assert.That(boolSetting!.Value?.GetValue(), Is.EqualTo(true));
            Assert.That(boolSetting.IsExternallyManaged, Is.True);
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallNotOverrideSettings_WhenClientOverridesDisabled()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: false, clientOverrideRegex: ".*"));
        
        SetEnvironmentVariable("StringSetting", "ShouldNotBeApplied");

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var stringSetting = client!.Settings.FirstOrDefault(a => a.Name == "StringSetting");
            Assert.That(stringSetting!.Value?.GetValue(), Is.EqualTo("OriginalString"), 
                "StringSetting should have the original default value when overrides are disabled");
            Assert.That(stringSetting.IsExternallyManaged, Is.False, 
                "StringSetting should not be marked as externally managed when overrides are disabled");
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallNotOverrideSettings_WhenClientNameDoesNotMatchRegex()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: "NonMatchingPattern"));
        
        SetEnvironmentVariable("StringSetting", "ShouldNotBeApplied");

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var stringSetting = client!.Settings.FirstOrDefault(a => a.Name == "StringSetting");
            Assert.That(stringSetting!.Value?.GetValue(), Is.EqualTo("OriginalString"), 
                "StringSetting should have the original default value when client name doesn't match regex");
            Assert.That(stringSetting.IsExternallyManaged, Is.False, 
                "StringSetting should not be marked as externally managed when client name doesn't match regex");
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallPreserveNonOverriddenSettings_WhenSomeSettingsOverridden()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        // Only override StringSetting, leave others at default
        SetEnvironmentVariable("StringSetting", "OnlyThisIsOverridden");

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var stringSetting = client!.Settings.FirstOrDefault(a => a.Name == "StringSetting");
            var intSetting = client.Settings.FirstOrDefault(a => a.Name == "IntSetting");
            var boolSetting = client.Settings.FirstOrDefault(a => a.Name == "BoolSetting");
            
            // StringSetting should be overridden and marked as externally managed
            Assert.That(stringSetting!.Value?.GetValue(), Is.EqualTo("OnlyThisIsOverridden"));
            Assert.That(stringSetting.IsExternallyManaged, Is.True);
            
            // IntSetting should have default value and NOT be externally managed
            Assert.That(intSetting!.Value?.GetValue(), Is.EqualTo(42)); // Default value
            Assert.That(intSetting.IsExternallyManaged, Is.False);
            
            // BoolSetting should have default value and NOT be externally managed
            Assert.That(boolSetting!.Value?.GetValue(), Is.EqualTo(false)); // Default value
            Assert.That(boolSetting.IsExternallyManaged, Is.False);
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallOverrideSettings_CaseInsensitive()
    {
        // Arrange
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        // Use different case for environment variable name
        SetEnvironmentVariable("STRINGSETTING", "CaseInsensitiveOverride");

        try
        {
            // Act
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert
            var clients = await GetAllClients();
            var client = clients.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(client, Is.Not.Null);

            var stringSetting = client!.Settings.FirstOrDefault(a => a.Name == "StringSetting");
            Assert.That(stringSetting!.Value?.GetValue(), Is.EqualTo("CaseInsensitiveOverride"), 
                "Environment variable matching should be case-insensitive");
            Assert.That(stringSetting.IsExternallyManaged, Is.True);
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    [Test]
    public async Task ShallNotTriggerUpdateEvents_WhenReregisteringWithUnchangedOverrides()
    {
        // Arrange - Enable client overrides
        await SetConfiguration(CreateConfiguration(allowClientOverrides: true, clientOverrideRegex: ".*"));
        
        SetEnvironmentVariable("StringSetting", "OverriddenValue");
        SetEnvironmentVariable("IntSetting", "42");

        try
        {
            // Act - Register client with overrides for the first time
            var secret = GetNewSecret();
            var (settings, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Get the client to verify initial state
            var clientsAfterFirst = await GetAllClients();
            var clientAfterFirst = clientsAfterFirst.FirstOrDefault(a => a.Name == settings.CurrentValue.ClientName);
            Assert.That(clientAfterFirst, Is.Not.Null);
            
            var stringSettingAfterFirst = clientAfterFirst!.Settings.FirstOrDefault(a => a.Name == "StringSetting");
            Assert.That(stringSettingAfterFirst!.Value?.GetValue(), Is.EqualTo("OverriddenValue"));
            Assert.That(stringSettingAfterFirst.IsExternallyManaged, Is.True);
            
            var intSettingAfterFirst = clientAfterFirst.Settings.FirstOrDefault(a => a.Name == "IntSetting");
            Assert.That(intSettingAfterFirst!.Value?.GetValue(), Is.EqualTo(42));
            Assert.That(intSettingAfterFirst.IsExternallyManaged, Is.True);
            
            // Get history count for StringSetting after first registration
            var historyAfterFirst = (await GetHistory(settings.CurrentValue.ClientName, "StringSetting", null)).ToList();
            var firstRegistrationHistoryCount = historyAfterFirst.Count;
            Assert.That(firstRegistrationHistoryCount, Is.GreaterThan(0), "Should have history after first registration");

            // Act - Re-register with the SAME override values
            var (settingsSecond, _) = InitializeConfigurationProvider<EnvironmentVariableOverrideSettings>(secret);

            // Assert - Verify no new history entries were created (values haven't changed)
            var historyAfterSecond = (await GetHistory(settingsSecond.CurrentValue.ClientName, "StringSetting", null)).ToList();
            Assert.That(historyAfterSecond.Count, Is.EqualTo(firstRegistrationHistoryCount), 
                "No new history entries should be created when re-registering with unchanged override values");
            
            // Verify the settings still have the correct values and are still marked as externally managed
            var clientsAfterSecond = await GetAllClients();
            var clientAfterSecond = clientsAfterSecond.FirstOrDefault(a => a.Name == settingsSecond.CurrentValue.ClientName);
            Assert.That(clientAfterSecond, Is.Not.Null);
            
            var stringSettingAfterSecond = clientAfterSecond!.Settings.FirstOrDefault(a => a.Name == "StringSetting");
            Assert.That(stringSettingAfterSecond!.Value?.GetValue(), Is.EqualTo("OverriddenValue"));
            Assert.That(stringSettingAfterSecond.IsExternallyManaged, Is.True);
            
            var intSettingAfterSecond = clientAfterSecond.Settings.FirstOrDefault(a => a.Name == "IntSetting");
            Assert.That(intSettingAfterSecond!.Value?.GetValue(), Is.EqualTo(42));
            Assert.That(intSettingAfterSecond.IsExternallyManaged, Is.True);
        }
        finally
        {
            CleanupEnvironmentVariables();
        }
    }

    #endregion

    private List<SettingDataContract> CreateOverrides()
    {
        return
        [
            new("AStringSetting", new StringSettingDataContract("bla")),
            new("AnIntSetting", new IntSettingDataContract(66)),
            new("ABoolSetting", new BoolSettingDataContract(false))
        ];
    }
}