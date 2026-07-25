using Fig.Api.Reports.Rendering.Components;

namespace Fig.Api.Reports;

public sealed class AiReportDocument : IReportDocumentMetadata
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IReadOnlyList<AiReportSection> Sections { get; set; } = [];
}

public abstract class AiReportSection
{
    public abstract string Type { get; }
}

public sealed class AiReportSummarySection : AiReportSection
{
    public override string Type => "summary";

    public IReadOnlyList<SummaryCardItem> Items { get; set; } = [];
}

public sealed class AiReportMarkdownSection : AiReportSection
{
    public override string Type => "markdown";

    public string Content { get; set; } = string.Empty;
}

public sealed class AiReportTableSection : AiReportSection
{
    public override string Type => "table";

    public string? Title { get; set; }

    public IReadOnlyList<string> Columns { get; set; } = [];

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];
}

public sealed class AiReportChartSection : AiReportSection
{
    public override string Type => "chart";

    public string? Title { get; set; }

    public string ChartType { get; set; } = "pie";

    public string DatasetLabel { get; set; } = "Value";

    public IReadOnlyList<ChartSlice> Data { get; set; } = [];
}

public sealed class AiReportTimelineSection : AiReportSection
{
    public override string Type => "timeline";

    public string? Title { get; set; }

    public IReadOnlyList<TimelineItem> Items { get; set; } = [];
}
