using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Contracts.Status;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class ApiVersionTests : IntegrationTestBase
{
    [Test]
    public async Task ShallUpdateLastUpdatedDateWhenSettingsAreUpdated()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var version1 = await GetApiVersion();
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("aNewValue"))
        };

        var start = DateTime.UtcNow;
        await SetSettings(settings.ClientName, settingsToUpdate);
        var end = DateTime.UtcNow;

        var version2 = await GetApiVersion();
        
        Assert.That(version1.ApiVersion, Is.EqualTo(version2.ApiVersion));
        Assert.That(version1.LastSettingChange, Is.LessThan(version2.LastSettingChange));
        Assert.That(version2.LastSettingChange, Is.InRange(start, end));
    }

    [Test]
    public async Task ShallUpdateApiVersionOnNewRegistration()
    {
        var start = DateTime.UtcNow;
        await RegisterSettings<ThreeSettings>();
        var end = DateTime.UtcNow;
        
        var version = await GetApiVersion();

        Assert.That(version.LastSettingChange, Is.InRange(start, end));
    }

    [Test]
    public async Task ShallUpdateApiVersionOnUpdatedRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientXWithTwoSettings>(secret);
        var version1 = await GetApiVersion();
        
        var start = DateTime.UtcNow;
        await RegisterSettings<ClientXWithThreeSettings>(secret);
        var end = DateTime.UtcNow;
        var version2 = await GetApiVersion();

        Assert.That(version1.LastSettingChange, Is.LessThan(version2.LastSettingChange));
        Assert.That(version2.LastSettingChange, Is.InRange(start, end));
    }

    [Test]
    public async Task ShallNotUpdateApiVersionOnIdenticalRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientXWithThreeSettings>(secret);
        var version1 = await GetApiVersion();
        
        await RegisterSettings<ClientXWithThreeSettings>(secret);
        var version2 = await GetApiVersion();

        Assert.That(version1.LastSettingChange, Is.EqualTo(version2.LastSettingChange));
    }
    
    private async Task<ApiVersionDataContract> GetApiVersion()
    {
        var uri = $"/apiversion";
        var result = await ApiClient.Get<ApiVersionDataContract>(uri);
        return result ?? throw new InvalidOperationException("API version response was null");
    }
}