using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.LookupTable;
using Fig.Mcp.Tools;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class LookupTableReadToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ListLookupTables_AfterCreatingTable_ReturnsTableInResults()
    {
        await AddLookupTable(new LookupTableDataContract(
            null,
            "TestTable",
            new Dictionary<string, string?> { { "key1", "val1" } },
            false));

        var result = await LookupTableReadTools.ListLookupTables(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("TestTable"));
        Assert.That(result, Does.Contain("key1"));
    }

    [Test]
    public async Task ListLookupTables_WhenNoTables_ReturnsEmptyArray()
    {
        var result = await LookupTableReadTools.ListLookupTables(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("[]"));
    }
}
