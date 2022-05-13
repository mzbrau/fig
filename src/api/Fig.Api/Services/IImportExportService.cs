using Fig.Api.DataImport;
using Fig.Contracts.ImportExport;

namespace Fig.Api.Services;

public interface IImportExportService
{
    Task<ImportResultDataContract> Import(FigDataExportDataContract data, ImportMode importMode);

    FigDataExportDataContract Export(bool decryptSecrets);
}