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

            var defaultClient = new ClientWithCultureBasedSettings();
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
            
            var defaultClient = new ClientWithCultureBasedSettings();
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

    private List<SettingDataContract> CreateOverrides()
    {
        return new List<SettingDataContract>
        {
            new("AStringSetting", new StringSettingDataContract("bla")),
            new("AnIntSetting", new IntSettingDataContract(66)),
            new("ABoolSetting", new BoolSettingDataContract(false))
        };
    }
}