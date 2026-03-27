using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.WebHook;
using Fig.Mcp.Tools;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class WebHookReadToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ListWebHooks_WhenEmpty_ReturnsEmptyArray()
    {
        var result = await WebHookReadTools.ListWebHooks(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("[]"));
    }

    [Test]
    public async Task ListWebHookClients_AfterCreatingClient_ReturnsClientInResults()
    {
        var whClient = new WebHookClientDataContract(
            null, "TestWHClient", new Uri("http://localhost/webhook"), "secret123");
        await CreateWebHookClient(whClient);

        var result = await WebHookReadTools.ListWebHookClients(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("TestWHClient"));
    }

    [Test]
    public async Task ListWebHookClients_WhenEmpty_ReturnsEmptyArray()
    {
        var result = await WebHookReadTools.ListWebHookClients(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("[]"));
    }

    [Test]
    public async Task ListWebHooks_AfterCreatingWebHook_ReturnsWebHookInResults()
    {
        var whClient = new WebHookClientDataContract(
            null, "WHClientForHook", new Uri("http://localhost/webhook"), "secret");
        var createdClient = await CreateWebHookClient(whClient);

        var webHook = new WebHookDataContract(
            null, createdClient.Id!.Value, WebHookType.SettingValueChanged, ".*", null, 0);
        await CreateWebHook(webHook);

        var result = await WebHookReadTools.ListWebHooks(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("WebHookType"));
    }
}
