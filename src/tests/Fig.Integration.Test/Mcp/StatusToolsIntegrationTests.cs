using System.Threading;
using System.Threading.Tasks;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class StatusToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task GetApiVersion_ReturnsVersionInfo()
    {
        var result = await StatusTools.GetApiVersion(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("Version"));
    }

    [Test]
    public async Task GetApiStatus_ReturnsNonEmptyStatus()
    {
        var result = await StatusTools.GetApiStatus(McpApiClient, CancellationToken.None);

        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetDeferredImports_ReturnsValidJsonArray()
    {
        var result = await StatusTools.GetDeferredImports(McpApiClient, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.StartWith("["));
    }

    [Test]
    public async Task GetClientRegistrationHistory_AfterRegistration_ContainsClientName()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var result = await StatusTools.GetClientRegistrationHistory(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain(settings.ClientName));
    }
}
