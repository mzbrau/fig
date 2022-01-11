using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using NUnit.Framework;

namespace Fig.Api.Integration.Test;

[TestFixture]
public class SettingsRegistrationTests : IntegrationTestBase
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
    public async Task ShallRegisterSingleClient()
    {
        var settings = await RegisterThreeSettings();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(clients.First().Name, Is.EqualTo(settings.ClientName));
        Assert.That(clients.First().Settings.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallRegisterMultipleClients()
    {
        await RegisterThreeSettings();
        await RegisterNoSettings();
        await RegisterClientXWithTwoSettings();
        await RegisterAllSettingsAndTypes();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count(), Is.EqualTo(4));

        var clientNames = string.Join(",", clients.Select(a => a.Name).OrderBy(a => a));
        Assert.That(clientNames, Is.EqualTo("AllSettingsAndTypes,ClientX,NoSettings,ThreeSettings"));
    }

    [Test]
    public async Task ShallUpdateSettingsDefinitionToAddSettings()
    {
        await RegisterClientXWithTwoSettings();
        await RegisterClientXWithThreeSettings();

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
        await RegisterClientXWithThreeSettings();
        await RegisterClientXWithTwoSettings();

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
        var settings = await RegisterThreeSettings();

        const string updatedString = "Some new value";
        const int updatedInt = 99;

        var updatedSettings = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = updatedString
            },
            new()
            {
                Name = nameof(settings.AnIntSetting),
                Value = updatedInt
            }
        };

        await SetSettings(settings.ClientName, updatedSettings);
        await RegisterThreeSettings();

        var finalSettings = (await GetSettingsForClient(settings.ClientName, settings.ClientSecret)).ToList();

        Assert.That(finalSettings.Count, Is.EqualTo(3));
        Assert.That(finalSettings.FirstOrDefault(a => a.Name == nameof(settings.AStringSetting)).Value,
            Is.EqualTo(updatedString));
        Assert.That(finalSettings.FirstOrDefault(a => a.Name == nameof(settings.AnIntSetting)).Value,
            Is.EqualTo(updatedInt));
        Assert.That(finalSettings.FirstOrDefault(a => a.Name == nameof(settings.ABoolSetting)).Value,
            Is.True);
    }

    public async Task RegistrationShouldUpdateAllInstances()
    {
    }
}