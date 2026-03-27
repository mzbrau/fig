using System.Threading;
using System.Threading.Tasks;
using Fig.Mcp.Tools;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class SessionToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task GetRunSessions_ReturnsValidJson()
    {
        var result = await SessionTools.GetRunSessions(McpApiClient, CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(result));
    }
}
