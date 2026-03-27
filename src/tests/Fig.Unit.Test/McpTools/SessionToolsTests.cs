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
}
