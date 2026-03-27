using Fig.Contracts.LookupTable;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class LookupTableReadToolsTests
{
    [Test]
    public async Task ListLookupTables_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var tables = new List<LookupTableDataContract>
        {
            new LookupTableDataContract(
                Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                "EnvironmentMap",
                new Dictionary<string, string?> { { "dev", "Development" }, { "prod", "Production" } },
                false)
        };

        mock.Setup(x => x.GetLookupTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tables);

        var result = await LookupTableReadTools.ListLookupTables(mock.Object, CancellationToken.None);

        mock.Verify(x => x.GetLookupTablesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("EnvironmentMap"));
        Assert.That(result, Does.Contain("Development"));
        Assert.That(result, Does.Contain("Production"));
    }
}
