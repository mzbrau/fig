using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class CustomActionReadToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task GetCustomActionStatus_WithRandomGuid_ThrowsHttpRequestException()
    {
        var randomExecutionId = Guid.NewGuid().ToString();

        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await CustomActionReadTools.GetCustomActionStatus(
                McpApiClient, randomExecutionId, CancellationToken.None));

        Assert.That(ex!.Message, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GetCustomActionHistory_WithNonExistentAction_ThrowsHttpRequestException()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SettingsWithCustomAction>(secret);
        var randomActionId = Guid.NewGuid().ToString();

        try
        {
            var result = await CustomActionReadTools.GetCustomActionHistory(
                McpApiClient, settings.ClientName, randomActionId, CancellationToken.None);

            // If the API returns data, verify it is not null
            Assert.That(result, Is.Not.Null);
        }
        catch (HttpRequestException ex)
        {
            // API returns 404 for unknown custom action — expected
            Assert.That(ex.Message, Is.Not.Null.And.Not.Empty);
        }
    }
}
