using Fig.Contracts.LookupTable;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class LookupTableWriteToolsTests
{
    [Test]
    public async Task CreateLookupTable_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.CreateLookupTableAsync(It.IsAny<LookupTableDataContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var lookupJson = JsonConvert.SerializeObject(new Dictionary<string, string?> { { "key1", "value1" } });

        var result = await LookupTableWriteTools.CreateLookupTable(
            mock.Object, "TestTable", lookupJson, CancellationToken.None);

        mock.Verify(x => x.CreateLookupTableAsync(
            It.Is<LookupTableDataContract>(t => t.Name == "TestTable"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }

    [Test]
    public async Task UpdateLookupTable_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.UpdateLookupTableAsync(It.IsAny<Guid>(), It.IsAny<LookupTableDataContract>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tableId = "42000000-0000-0000-0000-000000000001";
        var lookupJson = JsonConvert.SerializeObject(new Dictionary<string, string?> { { "updated", "data" } });

        var result = await LookupTableWriteTools.UpdateLookupTable(
            mock.Object, tableId, "UpdatedTable", lookupJson, CancellationToken.None);

        mock.Verify(x => x.UpdateLookupTableAsync(
            Guid.Parse(tableId),
            It.Is<LookupTableDataContract>(t => t.Name == "UpdatedTable"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }

    [Test]
    public async Task DeleteLookupTable_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        mock.Setup(x => x.DeleteLookupTableAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tableId = "99000000-0000-0000-0000-000000000002";

        var result = await LookupTableWriteTools.DeleteLookupTable(
            mock.Object, tableId, CancellationToken.None);

        mock.Verify(x => x.DeleteLookupTableAsync(Guid.Parse(tableId), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("successfully").IgnoreCase);
    }
}
