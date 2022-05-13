using Fig.Contracts.ImportExport;

namespace Fig.Web.Facades;

public interface IDataFacade
{
    Task<ImportResultDataContract?> ImportSettings(FigDataExportDataContract data);

    Task<FigDataExportDataContract?> ExportSettings(bool decryptSecrets);
}