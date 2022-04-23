using Fig.Api.DataImport;
using Fig.Contracts.ImportExport;

namespace Fig.Api.Services;

public interface IImportExportService
{
    Task Import(FigDataExportDataContract data, ImportMode importMode);

    FigDataExportDataContract Export();
}