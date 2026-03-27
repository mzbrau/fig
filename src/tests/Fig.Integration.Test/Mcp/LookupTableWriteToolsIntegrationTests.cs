using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.LookupTable;
using Fig.Mcp.Tools;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class LookupTableWriteToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task CreateLookupTable_CreatesTableSuccessfully()
    {
        var lookupDataJson = "{\"alpha\": \"one\", \"beta\": \"two\"}";

        var result = await LookupTableWriteTools.CreateLookupTable(
            McpApiClient, "McpCreatedTable", lookupDataJson, CancellationToken.None);

        Assert.That(result, Does.Contain("created successfully"));

        var tables = await GetAllLookupTables();
        Assert.That(tables, Has.Count.EqualTo(1));
        Assert.That(tables[0].Name, Is.EqualTo("McpCreatedTable"));
        Assert.That(tables[0].LookupTable["alpha"], Is.EqualTo("one"));
    }

    [Test]
    public async Task UpdateLookupTable_UpdatesNameAndData()
    {
        await AddLookupTable(new LookupTableDataContract(
            null, "OriginalName",
            new Dictionary<string, string?> { { "k1", "v1" } },
            false));

        var tables = await GetAllLookupTables();
        var tableId = tables.First().Id!.Value.ToString();

        var newDataJson = "{\"k2\": \"v2\"}";

        var result = await LookupTableWriteTools.UpdateLookupTable(
            McpApiClient, tableId, "UpdatedName", newDataJson, CancellationToken.None);

        Assert.That(result, Does.Contain("updated successfully"));

        var updatedTables = await GetAllLookupTables();
        Assert.That(updatedTables.First().Name, Is.EqualTo("UpdatedName"));
        Assert.That(updatedTables.First().LookupTable.ContainsKey("k2"), Is.True);
    }

    [Test]
    public async Task DeleteLookupTable_RemovesTable()
    {
        await AddLookupTable(new LookupTableDataContract(
            null, "ToDelete",
            new Dictionary<string, string?> { { "x", "y" } },
            false));

        var tables = await GetAllLookupTables();
        var tableId = tables.First().Id!.Value.ToString();

        var result = await LookupTableWriteTools.DeleteLookupTable(
            McpApiClient, tableId, CancellationToken.None);

        Assert.That(result, Does.Contain("deleted successfully"));

        var remaining = await GetAllLookupTables();
        Assert.That(remaining, Is.Empty);
    }
}
