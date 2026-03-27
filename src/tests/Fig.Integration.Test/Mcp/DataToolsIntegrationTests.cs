using System.Threading;
using System.Threading.Tasks;
using Fig.Mcp.Tools;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class DataToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ExportAllData_WithRegisteredClient_ContainsClientName()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var result = await DataTools.ExportAllData(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain(settings.ClientName));
    }

    [Test]
    public async Task ImportAllData_WithExportedData_SucceedsWithoutError()
    {
        var clientSecret = GetNewSecret();
        await RegisterSettings<ClientA>(clientSecret);

        var exportResult = await DataTools.ExportAllData(McpApiClient, CancellationToken.None);

        var result = await DataTools.ImportAllData(
            McpApiClient, exportResult, CancellationToken.None);

        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public async Task ExportValuesOnly_WithRegisteredClient_ContainsClientName()
    {
        var clientSecret = GetNewSecret();
        var settings = await RegisterSettings<ClientA>(clientSecret);

        var result = await DataTools.ExportValuesOnly(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain(settings.ClientName));
    }

    [Test]
    public async Task ImportValuesOnly_WithExportedValues_SucceedsWithoutError()
    {
        var clientSecret = GetNewSecret();
        await RegisterSettings<ClientA>(clientSecret);

        var exportResult = await DataTools.ExportValuesOnly(McpApiClient, CancellationToken.None);

        var result = await DataTools.ImportValuesOnly(
            McpApiClient, exportResult, CancellationToken.None);

        Assert.That(result, Is.Not.Empty);
    }

    [Test]
    public async Task DeleteDeferredImports_CompletesWithoutError()
    {
        var result = await DataTools.DeleteDeferredImports(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("deleted"));
    }
}
