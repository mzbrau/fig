using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.WebHook;
using Fig.Test.Common;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class WebHookTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAddWebHook()
    {
        var webHookToCreate = CreateTestWebHook();

        var webHook = await CreateWebHook(webHookToCreate);

        Assert.That(webHook.Id, Is.Not.Null);
        Assert.That(webHook.WebHookType, Is.EqualTo(webHookToCreate.WebHookType));
        Assert.That(webHook.ClientNameRegex, Is.EqualTo(webHookToCreate.ClientNameRegex));
        Assert.That(webHook.SettingNameRegex, Is.EqualTo(webHookToCreate.SettingNameRegex));
        Assert.That(webHook.MinSessions, Is.EqualTo(webHookToCreate.MinSessions));

        var webHooks = await GetAllWebHooks();
        
        Assert.That(webHooks.Count, Is.EqualTo(1));
        var firstWebHook = webHooks.Single();
        Assert.That(firstWebHook.WebHookType, Is.EqualTo(webHookToCreate.WebHookType));
        Assert.That(firstWebHook.ClientNameRegex, Is.EqualTo(webHookToCreate.ClientNameRegex));
        Assert.That(firstWebHook.SettingNameRegex, Is.EqualTo(webHookToCreate.SettingNameRegex));
        Assert.That(firstWebHook.MinSessions, Is.EqualTo(webHookToCreate.MinSessions));
    }

    [Test]
    public async Task ShallDeleteWebhook()
    {
        var webHookToCreate = CreateTestWebHook();
        var webHook = await CreateWebHook(webHookToCreate);

        await DeleteWebHook(webHook.Id!.Value);
        
        var webHooks = await GetAllWebHooks();
        
        Assert.That(webHooks.Count, Is.Zero);
    }

    [Test]
    public async Task ShallGetAllWebHooks()
    {
        var webHookToCreate1 = CreateTestWebHook("one", "https://localhost:9000");
        await CreateWebHook(webHookToCreate1);
        
        var webHookToCreate2 = CreateTestWebHook("two", "https://localhost:9001");
        await CreateWebHook(webHookToCreate2);
        
        var webHooks = await GetAllWebHooks();
        
        Assert.That(webHooks.Count, Is.EqualTo(2));
        Assert.That(webHooks[0].WebHookType, Is.EqualTo(webHookToCreate1.WebHookType));
        Assert.That(webHooks[0].ClientNameRegex, Is.EqualTo(webHookToCreate1.ClientNameRegex));
        Assert.That(webHooks[0].SettingNameRegex, Is.EqualTo(webHookToCreate1.SettingNameRegex));
        Assert.That(webHooks[0].MinSessions, Is.EqualTo(webHookToCreate1.MinSessions));
        Assert.That(webHooks[1].WebHookType, Is.EqualTo(webHookToCreate2.WebHookType));
        Assert.That(webHooks[1].ClientNameRegex, Is.EqualTo(webHookToCreate2.ClientNameRegex));
        Assert.That(webHooks[1].SettingNameRegex, Is.EqualTo(webHookToCreate2.SettingNameRegex));
        Assert.That(webHooks[1].MinSessions, Is.EqualTo(webHookToCreate2.MinSessions));

    }

    [Test]
    public async Task ShallUpdateWebHook()
    {
        var webHookToCreate = CreateTestWebHook();
        var webHook = await CreateWebHook(webHookToCreate);

        webHook.WebHookType = WebHookType.ConfigurationError;
        webHook.ClientNameRegex = "some updated regex";
        webHook.SettingNameRegex = "good setting";
        webHook.MinSessions = 2;
        var updated = await UpdateWebHook(webHook);
        
        Assert.That(updated.WebHookType, Is.EqualTo(webHook.WebHookType));
        Assert.That(updated.ClientNameRegex, Is.EqualTo(webHook.ClientNameRegex));
        Assert.That(updated.SettingNameRegex, Is.EqualTo(webHook.SettingNameRegex));
        Assert.That(updated.MinSessions, Is.EqualTo(webHook.MinSessions));
        
        var webHooks = await GetAllWebHooks();
        
        Assert.That(webHooks.Count, Is.EqualTo(1));
        Assert.That(webHooks[0].WebHookType, Is.EqualTo(webHook.WebHookType));
        Assert.That(webHooks[0].ClientNameRegex, Is.EqualTo(webHook.ClientNameRegex));
        Assert.That(webHooks[0].SettingNameRegex, Is.EqualTo(webHook.SettingNameRegex));
        Assert.That(webHooks[0].MinSessions, Is.EqualTo(webHook.MinSessions));
    }

    private async Task<WebHookDataContract> UpdateWebHook(WebHookDataContract webHook)
    {
        var uri = $"/webhooks/{Uri.EscapeDataString(webHook.Id.Value.ToString())}";
        var response = await ApiClient.Put<HttpResponseMessage>(uri, webHook);

        var result = await response?.Content.ReadAsStringAsync();

        if (result is null)
            throw new ApplicationException($"Null response when performing put to {uri}");
        
        return JsonConvert.DeserializeObject<WebHookDataContract>(result)!;
    }

    private WebHookDataContract CreateTestWebHook(string? name = null, string? uri = null)
    {
        return new WebHookDataContract(null, Guid.NewGuid(),
            WebHookType.SettingValueChanged, "Client1", "Setting1", 6);
    }
}