using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fig.Contracts.Json;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ClientsFigWebLoadWireTests : IntegrationTestBase
{
    [Test]
    public async Task GetAllClients_UsesFigWebLoadCompactJson_WithoutTypeMetadata()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(ApiClient.BearerToken!);

        var raw = await httpClient.GetStringAsync("/clients");

        Assert.That(raw, Does.Not.Contain("$type"),
            "GET /clients must use FigWebLoad compact discriminators, not Newtonsoft $type");
        Assert.That(raw, Does.Contain("\"t\":"),
            "GET /clients must include compact value discriminators");

        var clients = JsonConvert.DeserializeObject<SettingsClientDefinitionDataContract[]>(
            raw, FigWebLoadJsonSettings.Instance);

        Assert.That(clients, Is.Not.Null);
        Assert.That(clients!, Has.Length.GreaterThanOrEqualTo(1));

        var settings = clients.SelectMany(c => c.Settings).ToList();
        Assert.That(settings, Has.Count.GreaterThan(0));
        Assert.That(settings.Any(s => s.Value is StringSettingDataContract), Is.True);
        Assert.That(settings.All(s => s.ValueType is not null), Is.True);
    }

    [Test]
    public async Task GetAllClients_ViaApiClientHelper_DeserializesValues()
    {
        var registered = await RegisterSettings<AllSettingsAndTypes>();
        var clients = (await GetAllClients()).ToList();

        var client = clients.Single(c => c.Name == registered.ClientName);
        Assert.That(client.Settings, Has.Count.GreaterThan(0));
        Assert.That(client.Settings.Any(s => s.Value is not null), Is.True);
    }
}
