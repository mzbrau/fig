using Fig.Common.NetStandard.Json;
using Fig.Contracts.Settings;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class SettingWriteToolsTests
{
    private Mock<IFigApiClient> _apiClient = null!;

    [SetUp]
    public void SetUp()
    {
        _apiClient = new Mock<IFigApiClient>();
    }

    [Test]
    public async Task UpdateSettingValues_ShouldDeserializeJson_AndCallUpdateSettingsAsync()
    {
        const string clientName = "ServiceA";
        const string changeMessage = "Increased timeout";
        var settings = new List<SettingDataContract>
        {
            new("Timeout", null)
        };
        var settingsJson = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);

        var result = await SettingWriteTools.UpdateSettingValues(
            _apiClient.Object, clientName, settingsJson, changeMessage, CancellationToken.None);

        _apiClient.Verify(x => x.UpdateSettingsAsync(
            clientName,
            It.Is<IEnumerable<SettingDataContract>>(s => s.Any(x => x.Name == "Timeout")),
            changeMessage,
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("ServiceA"));
        Assert.That(result, Does.Contain("Successfully"));
    }

    [Test]
    public async Task UpdateSettingValues_WithNullChangeMessage_ShouldPassNullThrough()
    {
        var settings = new List<SettingDataContract> { new("Setting1", null) };
        var settingsJson = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);

        await SettingWriteTools.UpdateSettingValues(
            _apiClient.Object, "Client", settingsJson, null, CancellationToken.None);

        _apiClient.Verify(x => x.UpdateSettingsAsync(
            "Client",
            It.IsAny<IEnumerable<SettingDataContract>>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateSettingValues_WithNullJson_ShouldThrowArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await SettingWriteTools.UpdateSettingValues(
                _apiClient.Object, "Client", "null", null, CancellationToken.None));
    }

    [Test]
    public async Task ToggleLiveReload_Enable_ShouldCallSetLiveReloadAsync()
    {
        var sessionId = Guid.NewGuid();

        var result = await SettingWriteTools.ToggleLiveReload(
            _apiClient.Object, sessionId.ToString(), true, CancellationToken.None);

        _apiClient.Verify(x => x.SetLiveReloadAsync(sessionId, true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("enabled"));
        Assert.That(result, Does.Contain(sessionId.ToString()));
    }

    [Test]
    public async Task ToggleLiveReload_Disable_ShouldCallSetLiveReloadAsync()
    {
        var sessionId = Guid.NewGuid();

        var result = await SettingWriteTools.ToggleLiveReload(
            _apiClient.Object, sessionId.ToString(), false, CancellationToken.None);

        _apiClient.Verify(x => x.SetLiveReloadAsync(sessionId, false, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("disabled"));
    }

    [Test]
    public async Task RequestClientRestart_ShouldCallRestartSessionAsync()
    {
        var sessionId = Guid.NewGuid();

        var result = await SettingWriteTools.RequestClientRestart(
            _apiClient.Object, sessionId.ToString(), CancellationToken.None);

        _apiClient.Verify(x => x.RestartSessionAsync(sessionId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("Restart"));
        Assert.That(result, Does.Contain(sessionId.ToString()));
    }

    [Test]
    public void RequestClientRestart_WithInvalidGuid_ShouldThrowFormatException()
    {
        Assert.ThrowsAsync<FormatException>(async () =>
            await SettingWriteTools.RequestClientRestart(
                _apiClient.Object, "not-a-guid", CancellationToken.None));
    }

    [Test]
    public async Task UpdateSettingValues_WithMultipleSettings_ShouldDeserializeAll()
    {
        var settings = new List<SettingDataContract>
        {
            new("Setting1", null),
            new("Setting2", null),
            new("Setting3", null)
        };
        var settingsJson = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);

        await SettingWriteTools.UpdateSettingValues(
            _apiClient.Object, "Client", settingsJson, "batch update", CancellationToken.None);

        _apiClient.Verify(x => x.UpdateSettingsAsync(
            "Client",
            It.Is<IEnumerable<SettingDataContract>>(s => s.Count() == 3),
            "batch update",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
