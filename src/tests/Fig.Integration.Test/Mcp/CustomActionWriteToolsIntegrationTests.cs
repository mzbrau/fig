using System.Threading;
using System.Threading.Tasks;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class CustomActionWriteToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ExecuteCustomAction_WhenNoActiveSession_ReturnsResult()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var result = await CustomActionWriteTools.ExecuteCustomAction(
            McpApiClient,
            settings.ClientName,
            "SomeAction",
            null,
            CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }
}
