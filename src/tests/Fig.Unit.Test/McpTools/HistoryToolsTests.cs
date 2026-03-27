using Fig.Contracts.Settings;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class HistoryToolsTests
{
    private Mock<IFigApiClient> _apiClient = null!;

    [SetUp]
    public void SetUp()
    {
        _apiClient = new Mock<IFigApiClient>();
    }

    [Test]
    public async Task GetSettingHistory_ShouldCallApiWithAllParameters()
    {
        const string clientName = "ServiceA";
        const string settingName = "Timeout";
        const string instance = "prod";
        var history = new List<SettingValueDataContract>
        {
            new("Timeout", "30", DateTime.UtcNow.AddDays(-1), "admin", "Initial"),
            new("Timeout", "60", DateTime.UtcNow, "admin", "Increased")
        };
        _apiClient.Setup(x => x.GetSettingHistoryAsync(clientName, settingName, instance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = await HistoryTools.GetSettingHistory(
            _apiClient.Object, clientName, settingName, instance, CancellationToken.None);

        _apiClient.Verify(x => x.GetSettingHistoryAsync(
            clientName, settingName, instance, It.IsAny<CancellationToken>()), Times.Once);
        var deserialized = JsonConvert.DeserializeObject<List<SettingValueDataContract>>(result);
        Assert.That(deserialized, Has.Count.EqualTo(2));
        Assert.That(deserialized![0].Value, Is.EqualTo("30"));
        Assert.That(deserialized[1].Value, Is.EqualTo("60"));
    }

    [Test]
    public async Task GetSettingHistory_WithNullInstance_ShouldPassNullThrough()
    {
        const string clientName = "ServiceA";
        const string settingName = "Enabled";
        _apiClient.Setup(x => x.GetSettingHistoryAsync(clientName, settingName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SettingValueDataContract>());

        await HistoryTools.GetSettingHistory(
            _apiClient.Object, clientName, settingName, null, CancellationToken.None);

        _apiClient.Verify(x => x.GetSettingHistoryAsync(
            clientName, settingName, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetSettingHistory_WhenEmpty_ShouldReturnEmptyArray()
    {
        _apiClient.Setup(x => x.GetSettingHistoryAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SettingValueDataContract>());

        var result = await HistoryTools.GetSettingHistory(
            _apiClient.Object, "Client", "Setting", null, CancellationToken.None);

        var deserialized = JsonConvert.DeserializeObject<List<SettingValueDataContract>>(result);
        Assert.That(deserialized, Is.Empty);
    }

    [Test]
    public async Task GetLastChanged_ShouldReturnRawString()
    {
        const string rawJson = "{\"ServiceA\":{\"Timeout\":\"2024-01-15T10:30:00Z\"}}";
        _apiClient.Setup(x => x.GetLastChangedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rawJson);

        var result = await HistoryTools.GetLastChanged(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetLastChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Is.EqualTo(rawJson));
    }
}
