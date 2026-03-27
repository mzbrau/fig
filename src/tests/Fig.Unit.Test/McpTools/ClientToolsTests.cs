using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class ClientToolsTests
{
    private Mock<IFigApiClient> _apiClient = null!;

    [SetUp]
    public void SetUp()
    {
        _apiClient = new Mock<IFigApiClient>();
    }

    [Test]
    public async Task ListClients_ShouldCallGetClientsAsync_AndReturnSerializedJson()
    {
        var clients = new List<SettingsClientDefinitionDataContract>
        {
            new("ServiceA", "First service", null, false,
                new List<SettingDefinitionDataContract>(),
                Enumerable.Empty<SettingDataContract>()),
            new("ServiceB", "Second service", "prod", false,
                new List<SettingDefinitionDataContract>(),
                Enumerable.Empty<SettingDataContract>())
        };
        _apiClient.Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(clients);

        var result = await ClientTools.ListClients(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetClientsAsync(It.IsAny<CancellationToken>()), Times.Once);
        var deserialized = JsonConvert.DeserializeObject<List<SettingsClientDefinitionDataContract>>(result);
        Assert.That(deserialized, Has.Count.EqualTo(2));
        Assert.That(deserialized![0].Name, Is.EqualTo("ServiceA"));
        Assert.That(deserialized[1].Name, Is.EqualTo("ServiceB"));
        Assert.That(deserialized[1].Instance, Is.EqualTo("prod"));
    }

    [Test]
    public async Task ListClients_WhenNoClients_ShouldReturnEmptyArray()
    {
        _apiClient.Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettingsClientDefinitionDataContract>());

        var result = await ClientTools.ListClients(_apiClient.Object, CancellationToken.None);

        var deserialized = JsonConvert.DeserializeObject<List<SettingsClientDefinitionDataContract>>(result);
        Assert.That(deserialized, Is.Empty);
    }

    [Test]
    public async Task GetClientDescriptions_ShouldCallGetClientDescriptionsAsync_AndReturnSerializedJson()
    {
        var descriptions = new ClientsDescriptionDataContract(new[]
        {
            new ClientDescriptionDataContract("ServiceA", "First service"),
            new ClientDescriptionDataContract("ServiceB", "Second service")
        });
        _apiClient.Setup(x => x.GetClientDescriptionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(descriptions);

        var result = await ClientTools.GetClientDescriptions(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetClientDescriptionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        var deserialized = JsonConvert.DeserializeObject<ClientsDescriptionDataContract>(result);
        Assert.That(deserialized!.Clients, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ListClients_ShouldReturnValidJson()
    {
        var clients = new List<SettingsClientDefinitionDataContract>
        {
            new("TestClient", "A test client", null, false,
                new List<SettingDefinitionDataContract>(),
                Enumerable.Empty<SettingDataContract>(),
                clientVersion: "1.0.0")
        };
        _apiClient.Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(clients);

        var result = await ClientTools.ListClients(_apiClient.Object, CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(result));
    }

    [Test]
    public async Task ListClients_ShouldPassCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _apiClient.Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettingsClientDefinitionDataContract>());

        await ClientTools.ListClients(_apiClient.Object, cts.Token);

        _apiClient.Verify(x => x.GetClientsAsync(cts.Token), Times.Once);
    }
}
