namespace Fig.Api.Reports;

/// <summary>
/// When a report model implements this, the HTML header uses these values instead of the static report definition.
/// </summary>
public interface IReportDocumentMetadata
{
    string Title { get; }

    string? Description { get; }
}
