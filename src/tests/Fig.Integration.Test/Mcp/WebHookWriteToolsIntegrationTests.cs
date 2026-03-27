using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.WebHook;
using Fig.Mcp.Tools;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class WebHookWriteToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task CreateWebHookClient_CreatesClientSuccessfully()
    {
        var result = await WebHookWriteTools.CreateWebHookClient(
            McpApiClient, "McpWHClient", "http://localhost/webhook", "secret",
            CancellationToken.None);

        Assert.That(result, Does.Contain("created successfully"));

        var clients = await GetAllWebHookClients();
        Assert.That(clients, Has.Count.EqualTo(1));
        Assert.That(clients[0].Name, Is.EqualTo("McpWHClient"));
    }

    [Test]
    public async Task UpdateWebHookClient_UpdatesClientSuccessfully()
    {
        var whClient = new WebHookClientDataContract(
            null, "OrigClient", new Uri("http://localhost/webhook"), "secret");
        var created = await CreateWebHookClient(whClient);
        var clientId = created.Id!.Value.ToString();

        var result = await WebHookWriteTools.UpdateWebHookClient(
            McpApiClient, clientId, "RenamedClient", "http://localhost/webhook2", "newsecret",
            CancellationToken.None);

        Assert.That(result, Does.Contain("updated successfully"));

        var clients = await GetAllWebHookClients();
        Assert.That(clients.First().Name, Is.EqualTo("RenamedClient"));
    }

    [Test]
    public async Task DeleteWebHookClient_RemovesClient()
    {
        var whClient = new WebHookClientDataContract(
            null, "ToDeleteClient", new Uri("http://localhost/webhook"), "secret");
        var created = await CreateWebHookClient(whClient);
        var clientId = created.Id!.Value.ToString();

        var result = await WebHookWriteTools.DeleteWebHookClient(
            McpApiClient, clientId, CancellationToken.None);

        Assert.That(result, Does.Contain("deleted successfully"));

        var clients = await GetAllWebHookClients();
        Assert.That(clients, Is.Empty);
    }

    [Test]
    public async Task CreateWebHook_CreatesWebHookSuccessfully()
    {
        var whClient = new WebHookClientDataContract(
            null, "WHClientForCreate", new Uri("http://localhost/webhook"), "secret");
        var createdClient = await CreateWebHookClient(whClient);
        var clientGuid = createdClient.Id!.Value.ToString();

        var result = await WebHookWriteTools.CreateWebHook(
            McpApiClient, clientGuid, "SettingValueChanged", ".*", null, 0,
            CancellationToken.None);

        Assert.That(result, Does.Contain("created successfully"));

        var webHooks = await GetAllWebHooks();
        Assert.That(webHooks, Has.Count.EqualTo(1));
        Assert.That(webHooks[0].WebHookType, Is.EqualTo(WebHookType.SettingValueChanged));
    }

    [Test]
    public async Task UpdateWebHook_UpdatesWebHookSuccessfully()
    {
        var whClient = new WebHookClientDataContract(
            null, "WHClientForUpdate", new Uri("http://localhost/webhook"), "secret");
        var createdClient = await CreateWebHookClient(whClient);
        var clientGuid = createdClient.Id!.Value.ToString();

        var webHook = new WebHookDataContract(
            null, createdClient.Id!.Value, WebHookType.SettingValueChanged, ".*", null, 0);
        await CreateWebHook(webHook);

        var webHooks = await GetAllWebHooks();
        var webHookId = webHooks.First().Id!.Value.ToString();

        var result = await WebHookWriteTools.UpdateWebHook(
            McpApiClient, webHookId, clientGuid, "ClientStatusChanged", "TestClient.*", "Setting.*", 1,
            CancellationToken.None);

        Assert.That(result, Does.Contain("updated successfully"));

        var updated = await GetAllWebHooks();
        Assert.That(updated.First().WebHookType, Is.EqualTo(WebHookType.ClientStatusChanged));
        Assert.That(updated.First().ClientNameRegex, Is.EqualTo("TestClient.*"));
    }

    [Test]
    public async Task DeleteWebHook_RemovesWebHook()
    {
        var whClient = new WebHookClientDataContract(
            null, "WHClientForDelete", new Uri("http://localhost/webhook"), "secret");
        var createdClient = await CreateWebHookClient(whClient);

        var webHook = new WebHookDataContract(
            null, createdClient.Id!.Value, WebHookType.NewClientRegistration, ".*", null, 0);
        await CreateWebHook(webHook);

        var webHooks = await GetAllWebHooks();
        var webHookId = webHooks.First().Id!.Value.ToString();

        var result = await WebHookWriteTools.DeleteWebHook(
            McpApiClient, webHookId, CancellationToken.None);

        Assert.That(result, Does.Contain("deleted successfully"));

        var remaining = await GetAllWebHooks();
        Assert.That(remaining, Is.Empty);
    }
}
