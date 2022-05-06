using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fig.Contracts.Settings;
using Fig.Contracts.Status;
using Fig.Integration.Test.Api.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class ClientStatusTests : IntegrationTestBase
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
    public async Task ShallSetDefaultValues()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 5000,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };
        var status = await GetStatus(settings.ClientName, secret, clientStatus);

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
            new()
            {
                Name = nameof(settings.AStringSetting),
                Value = "aNewValue"
            }
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var clientStatus = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = lastUpdate,
            PollIntervalMs = 5000,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };

        var status = await GetStatus(settings.ClientName, secret, clientStatus);

        Assert.That(status.SettingUpdateAvailable, Is.True);
    }

    [Test]
    public async Task ShallUpdateClientConfiguration()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 5000,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };

        await GetStatus(settings.ClientName, secret, clientStatus);

        var config = new ClientConfigurationDataContract
        {
            PollIntervalMs = 100,
            LiveReload = false,
            RunSessionId = clientStatus.RunSessionId
        };

        await SetConfiguration(settings.ClientName, config);

        var status = await GetStatus(settings.ClientName, secret, clientStatus);

        Assert.That(status.PollIntervalMs, Is.EqualTo(100));
        Assert.That(status.LiveReload, Is.False);
    }

    [Test]
    public async Task ShallGetAllInstances()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus1 = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 3000,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };

        await GetStatus(settings.ClientName, secret, clientStatus1);

        var clientStatus2 = new StatusRequestDataContract
        {
            UptimeSeconds = 600,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 3000,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };

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

        var clientStatus1 = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 50,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };

        await GetStatus(settings.ClientName, secret, clientStatus1);

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        var clientStatus2 = new StatusRequestDataContract
        {
            UptimeSeconds = 600,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalMs = 30000,
            LiveReload = true,
            RunSessionId = Guid.NewGuid()
        };

        await GetStatus(settings.ClientName, secret, clientStatus2);

        var statuses = (await GetAllStatuses()).ToList();

        Assert.That(statuses.Count, Is.EqualTo(1));
        Assert.That(statuses.Single().RunSessions.Count, Is.EqualTo(1));
    }

    protected async Task SetConfiguration(string clientName, ClientConfigurationDataContract configuration,
        string? instance = null, bool authenticate = true)
    {
        var json = JsonConvert.SerializeObject(configuration);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = $"/statuses/{HttpUtility.UrlEncode(clientName)}/configuration";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.PutAsync(requestUri, data);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Set of configuration should succeed. {error}");
    }

    protected async Task<IEnumerable<ClientStatusDataContract>> GetAllStatuses(bool authenticate = true)
    {
        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetStringAsync("/statuses");

        Assert.That(result, Is.Not.Null, "Get all statuses should succeed.");

        return JsonConvert.DeserializeObject<IEnumerable<ClientStatusDataContract>>(result);
    }
}