using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Settings;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class SettingWriteToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task UpdateSettingValues_WithValidSettings_UpdatesSuccessfully()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        const string newValue = "http://www.example.com";
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(ClientA.WebsiteAddress), new StringSettingDataContract(newValue))
        };
        var settingsJson = JsonConvert.SerializeObject(settingsToUpdate, JsonSettings.FigDefault);

        var result = await SettingWriteTools.UpdateSettingValues(
            McpApiClient, settings.ClientName, settingsJson, "Test update", CancellationToken.None);

        Assert.That(result, Does.Contain("Successfully"));

        var updatedSettings = await GetSettingsForClient(settings.ClientName, clientSecret);
        var websiteAddress = updatedSettings.First(a => a.Name == nameof(ClientA.WebsiteAddress));
        Assert.That(websiteAddress.Value?.GetValue()?.ToString(), Is.EqualTo(newValue));
    }

    [Test]
    public void ToggleLiveReload_WithInvalidSessionId_ThrowsHttpRequestException()
    {
        var fakeSessionId = Guid.NewGuid().ToString();

        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await SettingWriteTools.ToggleLiveReload(
                McpApiClient, fakeSessionId, true, CancellationToken.None));
    }

    [Test]
    public void RequestClientRestart_WithInvalidSessionId_ThrowsHttpRequestException()
    {
        var fakeSessionId = Guid.NewGuid().ToString();

        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await SettingWriteTools.RequestClientRestart(
                McpApiClient, fakeSessionId, CancellationToken.None));
    }
}
