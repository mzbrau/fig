using Fig.Contracts.ImportExport;
using Fig.Contracts.Settings;
using Fig.Web.Models.ImportExport;

namespace Fig.Web.Facades;

public interface IDataFacade
{
    List<DeferredImportClientModel> DeferredClients { get; }
    
    Task<ImportResultDataContract?> ImportSettings(FigDataExportDataContract data);

    Task<FigDataExportDataContract?> ExportSettings(bool includeLastChanged = false);

    Task<ImportResultDataContract?> ImportValueOnlySettings(FigValueOnlyDataExportDataContract data);

    Task<FigValueOnlyDataExportDataContract?> ExportValueOnlySettings(bool excludeEnvironmentSpecific = false, bool includeLastChanged = false);
    
    Task<FigValueOnlyDataExportDataContract?> ExportValueOnlySettings(List<string> selectedClientIdentifiers, bool excludeEnvironmentSpecific = false, bool includeLastChanged = false);
    
    Task<List<ClientSelectionModel>> GetAvailableClientsForExport();
    
    Task<FigValueOnlyDataExportDataContract?> ExportChangeSetSettings(FigValueOnlyDataExportDataContract referenceData, bool excludeEnvironmentSpecific = false);
    
    Task RefreshDeferredClients();

    Task<IEnumerable<SettingValueDataContract>?> GetLastChangedForAllSettings(string clientName, string? instance);
}