using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Fig.Contracts.Settings;
using Fig.Integration.Test.Api.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class SettingsHistoryTests : IntegrationTestBase
{
    [Test]
    public async Task ShallReturnInitialRegistration()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var history = (await GetHistory(settings.ClientName, secret, nameof(settings.AStringSetting))).ToList();

        Assert.That(history, Is.Not.Null);
        Assert.That(history.Count, Is.EqualTo(1));
        Assert.That(history.First().Name, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(history.First().ChangedBy, Is.EqualTo("REGISTRATION"));
    }
    
    [Test]
    public async Task ShallReturnAllHistory()
    {
        const string newValue = "some new value";
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), newValue)
        };

        await SetSettings(settings.ClientName, settingsToUpdate);
        
        var history = (await GetHistory(settings.ClientName, secret, nameof(settings.AStringSetting))).ToList();
        
        Assert.That(history.Count, Is.EqualTo(2));
        Assert.That(history.First().Name, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(history.First().Value, Is.EqualTo(newValue));
        Assert.That(history.First().ChangedBy, Is.EqualTo(UserName));
    }

    [Test]
    public async Task ShallReturnNotFoundForInvalidSetting()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var requestUri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/settings/invalid/history";

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetAsync(requestUri);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ShallReturnNotFoundForInvalidClient()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var requestUri = $"/clients/invalid/settings/{Uri.EscapeDataString(settings.AStringSetting)}/history";

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetAsync(requestUri);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ShallSupportHistoryForInstances()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        const string newValue = "A new value";
        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), newValue)
        };

        const string instanceName = "Instance1";
        await SetSettings(settings.ClientName, updatedSettings, instanceName);
        
        var instanceHistory = (await GetHistory(settings.ClientName, secret, nameof(settings.AStringSetting), instance: instanceName)).ToList();
        Assert.That(instanceHistory, Is.Not.Null);
        Assert.That(instanceHistory.Count, Is.EqualTo(2));
        Assert.That(instanceHistory.First().Name, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(instanceHistory.First().ChangedBy, Is.EqualTo(UserName));
        
        var history = (await GetHistory(settings.ClientName, secret, nameof(settings.AStringSetting))).ToList();

        Assert.That(history, Is.Not.Null);
        Assert.That(history.Count, Is.EqualTo(1));
        Assert.That(history.First().Name, Is.EqualTo(nameof(settings.AStringSetting)));
        Assert.That(history.First().ChangedBy, Is.EqualTo("REGISTRATION"));
    }
    
    [Test]
    public async Task ShallTakeAllHistoryFromOriginalSettingWhenInstanceCreated()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        const string valueBeforeInstance = "A new value";
        var updatedSettings = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), valueBeforeInstance)
        };

        const string instanceName = "Instance1";
        await SetSettings(settings.ClientName, updatedSettings);
        
        const string valueAfterInstance = "after instance";
        var updatedSettings2 = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), valueAfterInstance)
        };
        await SetSettings(settings.ClientName, updatedSettings2, instanceName);
        
        var instanceHistory = (await GetHistory(settings.ClientName, secret, nameof(settings.AStringSetting), instance: instanceName)).ToList();
        Assert.That(instanceHistory, Is.Not.Null);
        Assert.That(instanceHistory.Count, Is.EqualTo(3));
        Assert.That(string.Join(",", instanceHistory.OrderBy(a => a.ChangedAt).Select(a => a.Value)), Is.EqualTo($"Horse,{valueBeforeInstance},{valueAfterInstance}"));
    }
}