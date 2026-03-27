using Fig.Contracts.EventHistory;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class EventToolsTests
{
    private Mock<IFigApiClient> _apiClient = null!;

    [SetUp]
    public void SetUp()
    {
        _apiClient = new Mock<IFigApiClient>();
    }

    [Test]
    public async Task GetEvents_ShouldCallGetEventsAsync_WithCorrectTimeRange()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        var events = new EventLogCollectionDataContract(
            start, start, end,
            new List<EventLogDataContract>
            {
                new(DateTime.UtcNow, "ServiceA", null, "Timeout", "SettingValueUpdated",
                    "30", "60", "admin", "Increased timeout", "127.0.0.1", "host1")
            });
        _apiClient.Setup(x => x.GetEventsAsync(start, end, It.IsAny<CancellationToken>())).ReturnsAsync(events);

        var result = await EventTools.GetEvents(_apiClient.Object, start, end, CancellationToken.None);

        _apiClient.Verify(x => x.GetEventsAsync(start, end, It.IsAny<CancellationToken>()), Times.Once);
        var deserialized = JsonConvert.DeserializeObject<EventLogCollectionDataContract>(result);
        Assert.That(deserialized!.Events.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetEvents_ShouldPassParametersThrough()
    {
        var start = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2024, 6, 15, 18, 0, 0, DateTimeKind.Utc);
        var emptyEvents = new EventLogCollectionDataContract(
            start, start, end, Enumerable.Empty<EventLogDataContract>());
        _apiClient.Setup(x => x.GetEventsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyEvents);

        await EventTools.GetEvents(_apiClient.Object, start, end, CancellationToken.None);

        _apiClient.Verify(x => x.GetEventsAsync(start, end, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetEventCount_ShouldReturnWrappedCount()
    {
        var countData = new EventLogCountDataContract(42);
        _apiClient.Setup(x => x.GetEventCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(countData);

        var result = await EventTools.GetEventCount(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetEventCountAsync(It.IsAny<CancellationToken>()), Times.Once);
        var json = JObject.Parse(result);
        Assert.That(json["EventLogCount"]!.Value<long>(), Is.EqualTo(42));
    }

    [Test]
    public async Task GetEventCount_WhenZero_ShouldReturnZeroCount()
    {
        var countData = new EventLogCountDataContract(0);
        _apiClient.Setup(x => x.GetEventCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(countData);

        var result = await EventTools.GetEventCount(_apiClient.Object, CancellationToken.None);

        var json = JObject.Parse(result);
        Assert.That(json["EventLogCount"]!.Value<long>(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetClientTimeline_ShouldCallWithCorrectClientName()
    {
        const string clientName = "MyService";
        var now = DateTime.UtcNow;
        var timeline = new EventLogCollectionDataContract(
            now, now.AddHours(-1), now,
            new List<EventLogDataContract>
            {
                new(now, clientName, null, "Setting1", "SettingValueUpdated",
                    "old", "new", "admin", null, null, null)
            });
        _apiClient.Setup(x => x.GetClientTimelineAsync(clientName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeline);

        var result = await EventTools.GetClientTimeline(_apiClient.Object, clientName, CancellationToken.None);

        _apiClient.Verify(x => x.GetClientTimelineAsync(clientName, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("MyService"));
    }
}
