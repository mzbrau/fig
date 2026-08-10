using Fig.Contracts.Status;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class SessionToolsTests
{
    private Mock<IFigApiClient> _apiClient = null!;

    [SetUp]
    public void SetUp()
    {
        _apiClient = new Mock<IFigApiClient>();
    }

    [Test]
    public async Task GetRunSessions_ShouldCallGetRunSessionsAsync_AndReturnSerializedJson()
    {
        var sessions = new List<ClientStatusDataContract>
        {
            new("ServiceA", null, DateTime.UtcNow, DateTime.UtcNow,
                new List<ClientRunSessionDataContract>()),
            new("ServiceB", "prod", DateTime.UtcNow, DateTime.UtcNow,
                new List<ClientRunSessionDataContract>())
        };
        _apiClient.Setup(x => x.GetRunSessionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sessions);

        var result = await SessionTools.GetRunSessions(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetRunSessionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        var deserialized = JsonConvert.DeserializeObject<List<ClientStatusDataContract>>(result);
        Assert.That(deserialized, Has.Count.EqualTo(2));
        Assert.That(deserialized![0].Name, Is.EqualTo("ServiceA"));
        Assert.That(deserialized[1].Instance, Is.EqualTo("prod"));
    }

    [Test]
    public async Task GetRunSessions_WhenNoSessions_ShouldReturnEmptyArray()
    {
        _apiClient.Setup(x => x.GetRunSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ClientStatusDataContract>());

        var result = await SessionTools.GetRunSessions(_apiClient.Object, CancellationToken.None);

        var deserialized = JsonConvert.DeserializeObject<List<ClientStatusDataContract>>(result);
        Assert.That(deserialized, Is.Empty);
    }

    [Test]
    public async Task GetRunSessions_ShouldProduceValidJson()
    {
        _apiClient.Setup(x => x.GetRunSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClientStatusDataContract>
            {
                new("Svc", null, null, null, new List<ClientRunSessionDataContract>())
            });

        var result = await SessionTools.GetRunSessions(_apiClient.Object, CancellationToken.None);

        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(result));
    }

    [Test]
    public async Task GetCustomStatusProperties_ShouldCallApi_AndReturnSerializedJson()
    {
        var properties = new List<CustomStatusSessionPropertiesDataContract>
        {
            new("ServiceA", null, Guid.NewGuid(), DateTime.UtcNow,
                new CustomStatusPropertiesDataContract(
                [
                    new CustomStatusPropertyDataContract("QueueDepth", CustomStatusValueType.Long, 5L)
                ]))
        };
        _apiClient.Setup(x => x.GetCustomStatusPropertiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(properties);

        var result = await SessionTools.GetCustomStatusProperties(_apiClient.Object, null, null, CancellationToken.None);

        _apiClient.Verify(x => x.GetCustomStatusPropertiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var deserialized = JsonConvert.DeserializeObject<List<CustomStatusSessionPropertiesDataContract>>(result);
        Assert.That(deserialized, Has.Count.EqualTo(1));
        Assert.That(deserialized![0].ClientName, Is.EqualTo("ServiceA"));
    }

    [Test]
    public async Task GetCustomStatusProperties_WithClientName_ShouldCallFilteredApi()
    {
        _apiClient.Setup(x => x.GetCustomStatusPropertiesAsync("ServiceA", "prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<CustomStatusSessionPropertiesDataContract>());

        await SessionTools.GetCustomStatusProperties(_apiClient.Object, "ServiceA", "prod", CancellationToken.None);

        _apiClient.Verify(x => x.GetCustomStatusPropertiesAsync("ServiceA", "prod", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void GetCustomStatusProperties_WithInstanceButNoClientName_ShouldThrow()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await SessionTools.GetCustomStatusProperties(_apiClient.Object, null, "prod", CancellationToken.None));
    }
}
