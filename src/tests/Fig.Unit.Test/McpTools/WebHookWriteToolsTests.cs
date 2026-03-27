using Fig.Contracts.WebHook;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class WebHookWriteToolsTests
{
    [Test]
    public async Task CreateWebHook_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.CreateWebHookAsync(It.IsAny<WebHookDataContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clientId = "11111111-2222-3333-4444-555555555555";

        var result = await WebHookWriteTools.CreateWebHook(
            mock.Object, clientId, "SettingValueChanged", "MyApp.*", "ConnectionString", 2, CancellationToken.None);

        mock.Verify(x => x.CreateWebHookAsync(
            It.Is<WebHookDataContract>(w =>
                w.ClientNameRegex == "MyApp.*" &&
                w.WebHookType == WebHookType.SettingValueChanged &&
                w.MinSessions == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }

    [Test]
    public async Task UpdateWebHook_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.UpdateWebHookAsync(It.IsAny<Guid>(), It.IsAny<WebHookDataContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var webhookId = "10000000-0000-0000-0000-000000000001";
        var clientId = "11111111-2222-3333-4444-555555555555";

        var result = await WebHookWriteTools.UpdateWebHook(
            mock.Object, webhookId, clientId, "MinRunSessions", "Service.*", null, 5, CancellationToken.None);

        mock.Verify(x => x.UpdateWebHookAsync(
            Guid.Parse(webhookId),
            It.Is<WebHookDataContract>(w =>
                w.ClientNameRegex == "Service.*" &&
                w.WebHookType == WebHookType.MinRunSessions &&
                w.MinSessions == 5),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }

    [Test]
    public async Task DeleteWebHook_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.DeleteWebHookAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var webhookId = "25000000-0000-0000-0000-000000000002";

        var result = await WebHookWriteTools.DeleteWebHook(
            mock.Object, webhookId, CancellationToken.None);

        mock.Verify(x => x.DeleteWebHookAsync(Guid.Parse(webhookId), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }

    [Test]
    public async Task CreateWebHookClient_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.CreateWebHookClientAsync(It.IsAny<WebHookClientDataContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await WebHookWriteTools.CreateWebHookClient(
            mock.Object, "SlackNotifier", "https://hooks.slack.example.com/webhook", "secret123", CancellationToken.None);

        mock.Verify(x => x.CreateWebHookClientAsync(
            It.Is<WebHookClientDataContract>(c =>
                c.Name == "SlackNotifier" &&
                c.BaseUri == new Uri("https://hooks.slack.example.com/webhook")),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }

    [Test]
    public async Task UpdateWebHookClient_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.UpdateWebHookClientAsync(It.IsAny<Guid>(), It.IsAny<WebHookClientDataContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var whClientId = "c0000000-0000-0000-0000-000000000003";

        var result = await WebHookWriteTools.UpdateWebHookClient(
            mock.Object, whClientId, "UpdatedNotifier", "https://updated.example.com/hook", "newsecret", CancellationToken.None);

        mock.Verify(x => x.UpdateWebHookClientAsync(
            Guid.Parse(whClientId),
            It.Is<WebHookClientDataContract>(c =>
                c.Name == "UpdatedNotifier" &&
                c.BaseUri == new Uri("https://updated.example.com/hook")),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }

    [Test]
    public async Task DeleteWebHookClient_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.DeleteWebHookClientAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var whClientId = "d0000000-0000-0000-0000-000000000004";

        var result = await WebHookWriteTools.DeleteWebHookClient(
            mock.Object, whClientId, CancellationToken.None);

        mock.Verify(x => x.DeleteWebHookClientAsync(Guid.Parse(whClientId), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }

    [Test]
    public async Task TestWebHookClient_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.TestWebHookClientAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var whClientId = "e0000000-0000-0000-0000-000000000005";

        var result = await WebHookWriteTools.TestWebHookClient(
            mock.Object, whClientId, CancellationToken.None);

        mock.Verify(x => x.TestWebHookClientAsync(Guid.Parse(whClientId), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }
}
