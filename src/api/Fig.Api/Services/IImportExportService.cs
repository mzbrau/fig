using Fig.Api.DataImport;
using Fig.Contracts.ImportExport;

namespace Fig.Api.Services;

public interface IImportExportService : IAuthenticatedService
{
    ImportResultDataContract Import(FigDataExportDataContract? data, ImportMode importMode);

    FigDataExportDataContract Export(bool excludeSecrets);
    
    FigValueOnlyDataExportDataContract ValueOnlyExport(bool excludeSecrets);
    
    ImportResultDataContract ValueOnlyImport(FigValueOnlyDataExportDataContract? data, ImportMode importMode);
    
    List<DeferredImportClientDataContract> GetDeferredImportClients();
}