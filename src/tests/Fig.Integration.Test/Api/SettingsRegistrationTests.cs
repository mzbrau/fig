using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Integration.Test.Api.TestSettings;
using Newtonsoft.Json;
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
        await RegisterSettings<NoSettings>();
        await RegisterSettings<ClientXWithTwoSettings>();
        await RegisterSettings<AllSettingsAndTypes>();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count(), Is.EqualTo(4));

        var clientNames = string.Join(",", clients.Select(a => a.Name).OrderBy(a => a));
        Assert.That(clientNames, Is.EqualTo("AllSettingsAndTypes,ClientX,NoSettings,ThreeSettings"));
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
                .Select(a => $"{a.Name}:{a.Description}:{a.Value}"));
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
                .Select(a => $"{a.Name}:{a.Description}:{a.Value}"));
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
            new(nameof(settings.AStringSetting), updatedString),
            new(nameof(settings.AnIntSetting), updatedInt)
        };

        await SetSettings(settings.ClientName, updatedSettings);
        await RegisterSettings<ThreeSettings>(secret);

        var finalSettings = (await GetSettingsForClient(settings.ClientName, secret)).ToList();

        Assert.That(finalSettings.Count, Is.EqualTo(3));
        Assert.That(finalSettings.First(a => a.Name == nameof(settings.AStringSetting)).Value,
            Is.EqualTo(updatedString));
        Assert.That(finalSettings.First(a => a.Name == nameof(settings.AnIntSetting)).Value,
            Is.EqualTo(updatedInt));
        Assert.That(finalSettings.First(a => a.Name == nameof(settings.ABoolSetting)).Value,
            Is.True);
    }

    [Test]
    public async Task RegistrationShouldUpdateAllInstances()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ClientXWithThreeSettings>(secret);

        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.SingleStringSetting), "some new value")
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
        var dataContract = settings.CreateDataContract();
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
        var dataContract = settings.CreateDataContract();
        var json = JsonConvert.SerializeObject(dataContract);
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
}