using Fig.Contracts.ImportExport;

namespace Fig.Web.MarkdownReport;

public interface IMarkdownReportGenerator
{
    string GenerateReport(FigDataExportDataContract export, bool maskSecrets, bool includeAnalysis);
}