using Fig.Contracts.ImportExport;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class DataToolsTests
{
    [Test]
    public async Task ExportAllData_CallsApiAndReturnsSerializedData()
    {
        var mock = new Mock<IFigApiClient>();
        var exportData = new FigDataExportDataContract(
            DateTime.UtcNow, ImportType.ClearAndImport, 1, new List<SettingClientExportDataContract>());

        mock.Setup(x => x.ExportDataAsync())
            .ReturnsAsync(exportData);

        var result = await DataTools.ExportAllData(mock.Object, CancellationToken.None);

        mock.Verify(x => x.ExportDataAsync(), Times.Once);
        Assert.That(result, Does.Contain("\"ImportType\": 0"));
    }

    [Test]
    public async Task ImportAllData_DeserializesJsonAndCallsApi()
    {
        var mock = new Mock<IFigApiClient>();
        var importData = new FigDataExportDataContract(
            DateTime.UtcNow, ImportType.ReplaceExisting, 1, new List<SettingClientExportDataContract>());
        var dataJson = JsonConvert.SerializeObject(importData);
        var importResult = new ImportResultDataContract
        {
            ImportType = ImportType.ReplaceExisting,
            ImportedClients = new List<string> { "ServiceA", "ServiceB" },
            DeferredImportClients = new List<string>(),
            DeletedClients = new List<string>(),
            ErrorMessage = null
        };

        mock.Setup(x => x.ImportDataAsync(
                It.IsAny<FigDataExportDataContract>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(importResult);

        var result = await DataTools.ImportAllData(mock.Object, dataJson, CancellationToken.None);

        mock.Verify(x => x.ImportDataAsync(
            It.IsAny<FigDataExportDataContract>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("ServiceA"));
    }

    [Test]
    public async Task ExportValuesOnly_CallsApiAndReturnsSerializedData()
    {
        var mock = new Mock<IFigApiClient>();
        var exportData = new FigValueOnlyDataExportDataContract(
            DateTime.UtcNow, ImportType.AddNew, 1, false,
            new List<SettingClientValueExportDataContract>());

        mock.Setup(x => x.ExportValueOnlyDataAsync())
            .ReturnsAsync(exportData);

        var result = await DataTools.ExportValuesOnly(mock.Object, CancellationToken.None);

        mock.Verify(x => x.ExportValueOnlyDataAsync(), Times.Once);
        Assert.That(result, Does.Contain("\"ImportType\": 2"));
    }

    [Test]
    public async Task ImportValuesOnly_DeserializesJsonAndCallsApi()
    {
        var mock = new Mock<IFigApiClient>();
        var importData = new FigValueOnlyDataExportDataContract(
            DateTime.UtcNow, ImportType.AddNew, 1, false,
            new List<SettingClientValueExportDataContract>());
        var dataJson = JsonConvert.SerializeObject(importData);
        var importResult = new ImportResultDataContract
        {
            ImportType = ImportType.AddNew,
            ImportedClients = new List<string> { "ValueClient" },
            DeferredImportClients = new List<string>(),
            DeletedClients = new List<string>(),
            ErrorMessage = null
        };

        mock.Setup(x => x.ImportValueOnlyDataAsync(
                It.IsAny<FigValueOnlyDataExportDataContract>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(importResult);

        var result = await DataTools.ImportValuesOnly(mock.Object, dataJson, CancellationToken.None);

        mock.Verify(x => x.ImportValueOnlyDataAsync(
            It.IsAny<FigValueOnlyDataExportDataContract>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("ValueClient"));
    }

    [Test]
    public async Task DeleteDeferredImports_CallsApiAndReturnsConfirmation()
    {
        var mock = new Mock<IFigApiClient>();

        mock.Setup(x => x.DeleteDeferredImportsAsync())
            .Returns(Task.CompletedTask);

        var result = await DataTools.DeleteDeferredImports(mock.Object, CancellationToken.None);

        mock.Verify(x => x.DeleteDeferredImportsAsync(), Times.Once);
        Assert.That(result, Does.Contain("deferred").IgnoreCase);
    }
}
