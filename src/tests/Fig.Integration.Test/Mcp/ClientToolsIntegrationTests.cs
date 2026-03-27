using System.Threading;
using System.Threading.Tasks;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class ClientToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ListClients_AfterRegistration_ReturnsRegisteredClient()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var result = await ClientTools.ListClients(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain(settings.ClientName));
    }

    [Test]
    public async Task ListClients_WhenNoClientsRegistered_ReturnsEmptyArray()
    {
        var result = await ClientTools.ListClients(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("[]"));
    }

    [Test]
    public async Task GetClientDescriptions_AfterRegistration_ReturnsClientName()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var result = await ClientTools.GetClientDescriptions(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain(settings.ClientName));
    }
}
