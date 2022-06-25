using Fig.Contracts.ImportExport;

namespace Fig.Web.Utils;

public interface IMarkdownReportGenerator
{
    string GenerateReport(FigDataExportDataContract export, bool maskSecrets);
}