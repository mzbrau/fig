using Fig.Contracts.ImportExport;

namespace Fig.Web.Utils;

public interface IMarkdownExporter
{
    string CreateMarkdown(FigDataExportDataContract export, bool maskSecrets);
}