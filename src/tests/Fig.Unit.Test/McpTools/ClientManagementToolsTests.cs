using Fig.Contracts.SettingClients;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class ClientManagementToolsTests
{
    [Test]
    public async Task ChangeClientSecret_CallsApiAndReturnsSerializedResponse()
    {
        var mock = new Mock<IFigApiClient>();
        var expiryUtc = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var response = new ClientSecretChangeResponseDataContract("MyService", expiryUtc);

        mock.Setup(x => x.ChangeClientSecretAsync(
                It.IsAny<string>(),
                It.IsAny<ClientSecretChangeRequestDataContract>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await ClientManagementTools.ChangeClientSecret(
            mock.Object, "MyService", "newSuperSecret123", expiryUtc, CancellationToken.None);

        mock.Verify(x => x.ChangeClientSecretAsync(
            "MyService",
            It.Is<ClientSecretChangeRequestDataContract>(r =>
                r.NewSecret == "newSuperSecret123" &&
                r.OldSecretExpiryUtc == expiryUtc),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result, Does.Contain("MyService"));
    }

    [Test]
    public async Task DeleteClient_CallsApiAndReturnsConfirmation()
    {
        var mock = new Mock<IFigApiClient>();

        mock.Setup(x => x.DeleteClientAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await ClientDeleteTools.DeleteClient(
            mock.Object, "ObsoleteService", CancellationToken.None);

        mock.Verify(x => x.DeleteClientAsync(
            "ObsoleteService",
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result, Does.Contain("ObsoleteService").IgnoreCase);
    }
}
