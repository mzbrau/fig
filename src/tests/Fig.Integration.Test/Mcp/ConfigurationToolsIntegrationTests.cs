using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.Configuration;
using Fig.Mcp.Tools;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class ConfigurationToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task GetConfiguration_ReturnsConfigWithAllowNewRegistrations()
    {
        var result = await ConfigurationTools.GetConfiguration(
            McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("AllowNewRegistrations"));
    }

    [Test]
    public async Task UpdateConfiguration_ModifiesConfigValue()
    {
        var configJson = await ConfigurationTools.GetConfiguration(
            McpApiClient, CancellationToken.None);

        var config = JsonConvert.DeserializeObject<FigConfigurationDataContract>(configJson);
        Assert.That(config, Is.Not.Null);

        config!.AllowNewRegistrations = false;
        var updatedJson = JsonConvert.SerializeObject(config, Formatting.Indented);

        var updateResult = await ConfigurationTools.UpdateConfiguration(
            McpApiClient, updatedJson, CancellationToken.None);
        Assert.That(updateResult, Does.Contain("updated successfully"));

        var verifyJson = await ConfigurationTools.GetConfiguration(
            McpApiClient, CancellationToken.None);
        var verifyConfig = JsonConvert.DeserializeObject<FigConfigurationDataContract>(verifyJson);
        Assert.That(verifyConfig!.AllowNewRegistrations, Is.False);
    }
}
