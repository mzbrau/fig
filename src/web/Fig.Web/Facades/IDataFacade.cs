using Fig.Contracts.ImportExport;
using Fig.Web.Models.ImportExport;

namespace Fig.Web.Facades;

public interface IDataFacade
{
    List<DeferredImportClientModel> DeferredClients { get; }
    
    Task<ImportResultDataContract?> ImportSettings(FigDataExportDataContract data);

    Task<FigDataExportDataContract?> ExportSettings();

    Task<ImportResultDataContract?> ImportValueOnlySettings(FigValueOnlyDataExportDataContract data);

    Task<FigValueOnlyDataExportDataContract?> ExportValueOnlySettings(bool excludeEnvironmentSpecific = false);
    
    Task RefreshDeferredClients();
}