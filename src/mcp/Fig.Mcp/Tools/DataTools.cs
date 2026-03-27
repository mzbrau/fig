using System.ComponentModel;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.ImportExport;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class DataTools
{
    [McpServerTool, Description("Export complete Fig configuration data including all client definitions, settings, values, " +
        "and metadata. Use this to create backups, migrate configurations between environments, " +
        "or review the full state of the system. The exported data can be re-imported using import_all_data.")]
    public static async Task<string> ExportAllData(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var data = await apiClient.ExportDataAsync(cancellationToken);
        return JsonConvert.SerializeObject(data, Formatting.Indented, JsonSettings.FigDefault);
    }

    [McpServerTool, Description("Import a full Fig data export including client definitions and settings. " +
        "⚠️ WARNING: The import behavior depends on the ImportType field in the data: " +
        "ClearAndImport — DELETES ALL existing data and replaces it with the import; " +
        "ReplaceExisting — overwrites matching clients, keeps others; " +
        "AddNew — only adds clients that don't already exist. " +
        "Always create a backup using export_all_data before importing. " +
        "Provide the full JSON data export object as returned by export_all_data.")]
    public static async Task<string> ImportAllData(
        IFigApiClient apiClient,
        [Description("The full JSON data export object to import. Must match the FigDataExportDataContract structure as returned by export_all_data.")] string dataJson,
        CancellationToken cancellationToken)
    {
        var data = JsonConvert.DeserializeObject<FigDataExportDataContract>(dataJson, JsonSettings.FigDefault)
            ?? throw new ArgumentException("Failed to deserialize import data JSON. Ensure the format matches FigDataExportDataContract.");

        var result = await apiClient.ImportDataAsync(data, cancellationToken);
        return JsonConvert.SerializeObject(result, Formatting.Indented);
    }

    [McpServerTool, Description("Export only the current setting values for all clients, without definitions or metadata. " +
        "This produces a lightweight export suitable for transferring values between environments " +
        "where client definitions are already registered. " +
        "The exported data can be re-imported using import_values_only.")]
    public static async Task<string> ExportValuesOnly(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var data = await apiClient.ExportValueOnlyDataAsync(cancellationToken);
        return JsonConvert.SerializeObject(data, Formatting.Indented, JsonSettings.FigDefault);
    }

    [McpServerTool, Description("Import setting values only (no definitions). " +
        "The import behavior depends on the ImportType field: " +
        "UpdateValues — updates values for matching settings; " +
        "UpdateValuesInitOnly — only updates values for clients that have not yet registered. " +
        "Client definitions must already exist in the system for values to be applied. " +
        "Provide the full JSON data as returned by export_values_only.")]
    public static async Task<string> ImportValuesOnly(
        IFigApiClient apiClient,
        [Description("The full JSON values export object to import. Must match the FigValueOnlyDataExportDataContract structure as returned by export_values_only.")] string dataJson,
        CancellationToken cancellationToken)
    {
        var data = JsonConvert.DeserializeObject<FigValueOnlyDataExportDataContract>(dataJson, JsonSettings.FigDefault)
            ?? throw new ArgumentException("Failed to deserialize import data JSON. Ensure the format matches FigValueOnlyDataExportDataContract.");

        var result = await apiClient.ImportValueOnlyDataAsync(data, cancellationToken);
        return JsonConvert.SerializeObject(result, Formatting.Indented);
    }

    [McpServerTool, Description("Delete all pending deferred imports. " +
        "Deferred imports are value-only imports that are waiting for their target clients to register. " +
        "This clears all such pending imports so they will not be applied when clients register.")]
    public static async Task<string> DeleteDeferredImports(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        await apiClient.DeleteDeferredImportsAsync(cancellationToken);
        return "All pending deferred imports have been deleted.";
    }
}
