using Fig.Contracts.Reports;

namespace Fig.Api.Reports;

public class ReportRenderContext
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required DateTime GeneratedAtUtc { get; init; }

    public required string GeneratedBy { get; init; }

    public required IReadOnlyDictionary<string, string> ParameterSummary { get; init; }

    public required ReportPageOrientation PageOrientation { get; init; }

    public required Type BodyComponentType { get; init; }

    public required object Model { get; init; }
}
