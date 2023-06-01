using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.WebHook;
using Fig.Test.Common;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class WebHookClientTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAddWebHookClient()
    {
        var clientToCreate = CreateTestClient();

        var client = await CreateWebHookClient(clientToCreate);

        Assert.That(client.Id, Is.Not.Null);
        Assert.That(client.Name, Is.EqualTo(clientToCreate.Name));
        Assert.That(client.BaseUri, Is.EqualTo(clientToCreate.BaseUri));
        
        var clients = await GetAllWebHookClients();
        
        Assert.That(clients.Count, Is.EqualTo(1));
        var firstClient = clients.Single();
        Assert.That(firstClient.Name, Is.EqualTo(clientToCreate.Name));
        Assert.That(firstClient.BaseUri, Is.EqualTo(clientToCreate.BaseUri));
        Assert.That(firstClient.Id, Is.EqualTo(client.Id));
    }

    [Test]
    public async Task ShallDeleteWebhookClient()
    {
        var clientToCreate = CreateTestClient();
        var client = await CreateWebHookClient(clientToCreate);

        await DeleteWebHookClient(client.Id!.Value);
        
        var clients = await GetAllWebHookClients();
        
        Assert.That(clients.Count, Is.Zero);
    }

    [Test]
    public async Task ShallGetAllWebHookClients()
    {
        var clientToCreate1 = CreateTestClient("one", "https://localhost:9000");
        await CreateWebHookClient(clientToCreate1);
        
        var clientToCreate2 = CreateTestClient("two", "https://localhost:9001");
        await CreateWebHookClient(clientToCreate2);
        
        var clients = await GetAllWebHookClients();
        
        Assert.That(clients.Count, Is.EqualTo(2));
        Assert.That(clients[0].Name, Is.EqualTo(clientToCreate1.Name));
        Assert.That(clients[0].BaseUri, Is.EqualTo(clientToCreate1.BaseUri));
        Assert.That(clients[1].Name, Is.EqualTo(clientToCreate2.Name));
        Assert.That(clients[1].BaseUri, Is.EqualTo(clientToCreate2.BaseUri));
    }

    [Test]
    public async Task ShallUpdateWebHookClient()
    {
        var clientToCreate = CreateTestClient();
        var client = await CreateWebHookClient(clientToCreate);

        client.Name = "updated";
        client.BaseUri = new Uri("https://localhost:9002");
        var updated = await UpdateWebHookClient(client);
        
        Assert.That(updated.Name, Is.EqualTo(client.Name));
        Assert.That(updated.BaseUri, Is.EqualTo(client.BaseUri));
        
        var clients = await GetAllWebHookClients();
        
        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(clients[0].Name, Is.EqualTo(client.Name));
        Assert.That(clients[0].BaseUri, Is.EqualTo(client.BaseUri));
    }

    [Test]
    public async Task ShallPreventDeletionOfClientThatHasALinkedWebHook()
    {
        var clientToCreate = CreateTestClient();
        var client = await CreateWebHookClient(clientToCreate);

        var webHookToCreate = new WebHookDataContract(null, client.Id.Value, WebHookType.NewClientRegistration, ".*", ".*", 2);
        await CreateWebHook(webHookToCreate);

        var errorResult = await DeleteWebHookClient(client.Id!.Value, false);
        
        Assert.That(errorResult, Is.Not.Null);
        
        var clients = await GetAllWebHookClients();
        
        Assert.That(clients.Count, Is.Not.Zero);
    }

    private async Task<WebHookClientDataContract> UpdateWebHookClient(WebHookClientDataContract client)
    {
        var uri = $"/webhookclient/{Uri.EscapeDataString(client.Id.Value.ToString())}";
        var response = await ApiClient.Put<HttpResponseMessage>(uri, client);

        var result = await response?.Content.ReadAsStringAsync();

        if (result is null)
            throw new ApplicationException($"Null response when performing put to {uri}");
        
        return JsonConvert.DeserializeObject<WebHookClientDataContract>(result)!;
    }
    
    private async Task<WebHookClientDataContract> CreateWebHookClient(WebHookClientDataContract client)
    {
        const string uri = "/webhookclient";
        var response = await ApiClient.Post(uri, client, authenticate: true);

        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<WebHookClientDataContract>(result);
    }

    private WebHookClientDataContract CreateTestClient(string? name = null, string? uri = null)
    {
        return new WebHookClientDataContract(null, 
            name ?? "TestClient",
            uri != null ? new Uri(uri) : new Uri("https://localhost:9000"), 
            "ABCXYZ");
    }
}