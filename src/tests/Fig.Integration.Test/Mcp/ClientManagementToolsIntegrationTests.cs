using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class ClientManagementToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ChangeClientSecret_ForRegisteredClient_ReturnsSecretChangeDetails()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var result = await ClientManagementTools.ChangeClientSecret(
            McpApiClient,
            settings.ClientName,
            "NewS3cret!Complex#Pass_" + Guid.NewGuid().ToString("N"),
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        Assert.That(result, Does.Contain("OldSecretExpiryUtc"));
    }

    [Test]
    public async Task DeleteClient_ForRegisteredClient_RemovesClient()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var clients = await GetAllClients();
        Assert.That(clients.Any(c => c.Name == settings.ClientName), Is.True);

        await ClientDeleteTools.DeleteClient(
            McpApiClient, settings.ClientName, CancellationToken.None);

        var clientsAfter = await GetAllClients();
        Assert.That(clientsAfter.Any(c => c.Name == settings.ClientName), Is.False);
    }
}
