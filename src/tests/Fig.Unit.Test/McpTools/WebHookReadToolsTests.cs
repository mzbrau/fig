using Fig.Contracts.WebHook;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class WebHookReadToolsTests
{
    [Test]
    public async Task ListWebHooks_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var clientId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var webHooks = new List<WebHookDataContract>
        {
            new WebHookDataContract(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                clientId,
                WebHookType.SettingValueChanged,
                "MyApp.*",
                "ConnectionString",
                3)
        };

        mock.Setup(x => x.GetWebHooksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(webHooks);

        var result = await WebHookReadTools.ListWebHooks(mock.Object, CancellationToken.None);

        mock.Verify(x => x.GetWebHooksAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("MyApp.*"));
        Assert.That(result, Does.Contain("\"WebHookType\": 1"));
        Assert.That(result, Does.Contain("ConnectionString"));
    }

    [Test]
    public async Task ListWebHookClients_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var clients = new List<WebHookClientDataContract>
        {
            new WebHookClientDataContract(
                Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000"),
                "SlackNotifier",
                new Uri("https://hooks.slack.example.com/webhook"),
                "my-secret-key")
        };

        mock.Setup(x => x.GetWebHookClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(clients);

        var result = await WebHookReadTools.ListWebHookClients(mock.Object, CancellationToken.None);

        mock.Verify(x => x.GetWebHookClientsAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("SlackNotifier"));
        Assert.That(result, Does.Contain("hooks.slack.example.com"));
    }
}
