using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class SettingsHistoryTests : IntegrationTestBase
{
    [Test]
    public async Task ShallReturnInitialRegistration()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var history = (await GetHistory(settings.ClientName, nameof(settings.AStringSetting))).ToList();

        Assert.That(history, Is.Not.Null);
        Assert.That(history.Count, Is.EqualTo(1));
        Assert.That(history.First().Name, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(history.First().ChangedBy, Is.EqualTo("REGISTRATION"));
    }
    
    [Test]
    public async Task ShallReturnAllHistory()
    {
        const string newValue = "some new value";
        var settings = await RegisterSettings<ThreeSettings>();

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);
        
        var history = (await GetHistory(settings.ClientName, nameof(settings.AStringSetting))).ToList();
        
        Assert.That(history.Count, Is.EqualTo(2));
        Assert.That(history.First().Name, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(history.First().Value, Is.EqualTo(newValue));
        Assert.That(history.First().ChangedBy, Is.EqualTo(UserName));
    }

    [Test]
    public async Task ShallReturnNotFoundForInvalidSetting()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var requestUri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/settings/invalid/history";

        await ApiClient.GetAndVerify(requestUri, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallReturnNotFoundForInvalidClient()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var requestUri = $"/clients/invalid/settings/{Uri.EscapeDataString(nameof(settings.AStringSetting))}/history";

        await ApiClient.GetAndVerify(requestUri, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallSupportHistoryForInstances()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        const string newValue = "A new value";
        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(newValue))
        };

        const string instanceName = "Instance1";
        await SetSettings(settings.ClientName, updatedSettings, instanceName);
        
        var instanceHistory = (await GetHistory(settings.ClientName, nameof(settings.AStringSetting), instance: instanceName)).ToList();
        Assert.That(instanceHistory, Is.Not.Null);
        Assert.That(instanceHistory.Count, Is.EqualTo(2));
        Assert.That(instanceHistory.First().Name, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(instanceHistory.First().ChangedBy, Is.EqualTo(UserName));
        
        var history = (await GetHistory(settings.ClientName, nameof(settings.AStringSetting))).ToList();

        Assert.That(history, Is.Not.Null);
        Assert.That(history.Count, Is.EqualTo(1));
        Assert.That(history.First().Name, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(history.First().ChangedBy, Is.EqualTo("REGISTRATION"));
    }
    
    [Test]
    public async Task ShallTakeAllHistoryFromOriginalSettingWhenInstanceCreated()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        const string valueBeforeInstance = "A new value";
        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(valueBeforeInstance))
        };

        const string instanceName = "Instance1";
        await SetSettings(settings.ClientName, updatedSettings);
        
        const string valueAfterInstance = "after instance";
        var updatedSettings2 = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract(valueAfterInstance))
        };
        await SetSettings(settings.ClientName, updatedSettings2, instanceName);
        
        var instanceHistory = (await GetHistory(settings.ClientName, nameof(settings.AStringSetting), instance: instanceName)).ToList();
        Assert.That(instanceHistory, Is.Not.Null);
        Assert.That(instanceHistory.Count, Is.EqualTo(3));
        Assert.That(string.Join(",", instanceHistory.OrderBy(a => a.ChangedAt).Select(a => a.Value)), Is.EqualTo($"Horse,{valueBeforeInstance},{valueAfterInstance}"));
    }

    [Test]
    public async Task ShallReturnUnauthorizedIfClientIsNotAllowedForUser()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var uri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/settings/{Uri.EscapeDataString(nameof(settings.AStringSetting))}/history";

        using var httpClient = GetHttpClient();
        
        var user = NewUser(role: Role.Administrator, clientFilter: "notMatching");
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is null"));
        
        httpClient.DefaultRequestHeaders.Add("Authorization", loginResult.Token);
        
        var response = await httpClient.GetAsync(uri);
        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}