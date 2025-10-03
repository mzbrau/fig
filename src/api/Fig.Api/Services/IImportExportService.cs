using Fig.Api.DataImport;
using Fig.Contracts.ImportExport;

namespace Fig.Api.Services;

public interface IImportExportService : IAuthenticatedService
{
    Task<ImportResultDataContract> Import(FigDataExportDataContract? data, ImportMode importMode);

    Task<FigDataExportDataContract> Export(bool createEventLog = true);
    
    Task<FigValueOnlyDataExportDataContract> ValueOnlyExport(bool excludeEnvironmentSpecific = false);
    
    Task<ImportResultDataContract> ValueOnlyImport(FigValueOnlyDataExportDataContract? data, ImportMode importMode);
    
    Task<List<DeferredImportClientDataContract>> GetDeferredImportClients();

    Task DeleteAllDeferredImports();
}