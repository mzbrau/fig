using System;
using System.Collections.Generic;
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
        var settings = await RegisterSettings<ThreeSettings>();

        var clientStatus = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalSeconds = 45,
            LiveReload = true
        };
        var status = await GetStatus(settings.ClientName, settings.ClientSecret, clientStatus);
        
        Assert.That(status.LiveReload, Is.EqualTo(clientStatus.LiveReload));
        Assert.That(status.PollIntervalSeconds, Is.EqualTo(clientStatus.PollIntervalSeconds));
        Assert.That(status.SettingUpdateAvailable, Is.False);
    }

    [Test]
    public async Task ShallIdentifyWhenValuesAreOutdated()
    {
        var settings = await RegisterSettings<ThreeSettings>();

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
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalSeconds = 45,
            LiveReload = true
        };
        
        var status = await GetStatus(settings.ClientName, settings.ClientSecret, clientStatus);

        Assert.That(status.SettingUpdateAvailable, Is.True);
    }

    [Test]
    public async Task ShallUpdateClientConfiguration()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var config = new ClientConfigurationDataContract
        {
            PollIntervalSeconds = 100,
            LiveReload = false
        };

        await SetConfiguration(settings.ClientName, config);
        
        var clientStatus = new StatusRequestDataContract
        {
            UptimeSeconds = 500,
            LastSettingUpdate = DateTime.UtcNow,
            PollIntervalSeconds = 45,
            LiveReload = true
        };
        
        var status = await GetStatus(settings.ClientName, settings.ClientSecret, clientStatus);

        Assert.That(status.PollIntervalSeconds, Is.EqualTo(100));
        Assert.That(status.LiveReload, Is.False);
    }
    
    private async Task<StatusResponseDataContract> GetStatus(string clientName, string? clientSecret, StatusRequestDataContract status)
    {
        var json = JsonConvert.SerializeObject(status);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
        var response = await httpClient.PutAsync($"clients/{clientName}/status", data);

        var error = await GetErrorResult(response);
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Getting status should succeed. {error}");

        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<StatusResponseDataContract>(result);
    }

    protected async Task SetConfiguration(string clientName, ClientConfigurationDataContract configuration,
        string? instance = null, bool authenticate = true)
    {
        var json = JsonConvert.SerializeObject(configuration);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = $"/clients/{HttpUtility.UrlEncode(clientName)}/configuration";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.PutAsync(requestUri, data);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Set of configuration should succeed. {error}");
    }
}