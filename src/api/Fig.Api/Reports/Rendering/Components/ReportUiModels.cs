namespace Fig.Api.Reports.Rendering.Components;

public record SummaryCardItem(string Label, string Value, string? SubText = null);

public record ReportTableColumn(string Header, string PropertyName);

public record TimelineItem(DateTime TimestampUtc, string Title, string? Detail = null);

public record ChartSlice(string Label, double Value, string? Color = null);
