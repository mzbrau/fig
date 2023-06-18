using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Contracts.Status;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class ClientStatusTests : IntegrationTestBase
{
    [Test]
    public async Task ShallSetDefaultValues()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(500, DateTime.UtcNow, 5000, true);
        var status = await GetStatus("ThreeSettings", secret, clientStatus);

        Assert.That(status.LiveReload, Is.EqualTo(clientStatus.LiveReload));
        Assert.That(status.PollIntervalMs, Is.EqualTo(clientStatus.PollIntervalMs));
        Assert.That(status.SettingUpdateAvailable, Is.False);
    }

    [Test]
    public async Task ShallIdentifyWhenValuesAreOutdated()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var lastUpdate = DateTime.UtcNow;

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.AStringSetting), new StringSettingDataContract("aNewValue"))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var clientStatus = CreateStatusRequest(500, lastUpdate, 5000, true);

        var status = await GetStatus(settings.ClientName, secret, clientStatus);

        Assert.That(status.SettingUpdateAvailable, Is.True);
    }

    [Test]
    public async Task ShallUpdateClientConfiguration()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(500, DateTime.UtcNow, 5000, true);

        await GetStatus(settings.ClientName, secret, clientStatus);

        var config = new ClientConfigurationDataContract
        {
            PollIntervalMs = 100,
            LiveReload = false,
            RunSessionId = clientStatus.RunSessionId
        };

        var status = await SetConfiguration(settings.ClientName, config);
        
        Assert.That(status, Is.Not.Null);
        Assert.That(status!.PollIntervalMs, Is.EqualTo(100), "Second get should have updated value.");
        Assert.That(status.LiveReload, Is.False);
    }

    [Test]
    public async Task ShallGetAllInstances()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus1 = CreateStatusRequest(500, DateTime.UtcNow, 3000, true);

        await GetStatus(settings.ClientName, secret, clientStatus1);

        var clientStatus2 = CreateStatusRequest(600, DateTime.UtcNow, 3000, true);

        await GetStatus(settings.ClientName, secret, clientStatus2);

        var statuses = (await GetAllStatuses()).ToList();

        Assert.That(statuses.Count, Is.EqualTo(1));
        Assert.That(statuses.Single().RunSessions.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ShallRemoveExpiredSessions()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus1 = CreateStatusRequest(500, DateTime.UtcNow, 50, true);

        await GetStatus(settings.ClientName, secret, clientStatus1);

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        var clientStatus2 = CreateStatusRequest(600, DateTime.UtcNow, 30000, true);

        await GetStatus(settings.ClientName, secret, clientStatus2);

        var statuses = (await GetAllStatuses()).ToList();

        Assert.That(statuses.Count, Is.EqualTo(1));
        Assert.That(statuses.Single().RunSessions.Count, Is.EqualTo(1));
    }

    private async Task<ClientConfigurationDataContract?> SetConfiguration(string clientName, ClientConfigurationDataContract configuration,
        string? instance = null, bool authenticate = true)
    {
        var requestUri = $"/statuses/{Uri.EscapeDataString(clientName)}/configuration";
        if (instance != null) requestUri += $"?instance={Uri.EscapeDataString(instance)}";

        return await ApiClient.Put<ClientConfigurationDataContract>(requestUri, configuration, authenticate);
    }

    private async Task<IEnumerable<ClientStatusDataContract>> GetAllStatuses(bool authenticate = true)
    {
        const string uri = "/statuses";
        var result = await ApiClient.Get<IEnumerable<ClientStatusDataContract>>(uri);

        if (result is null)
            throw new ApplicationException($"Null result for get to uri {uri}");

        return result;
    }
}