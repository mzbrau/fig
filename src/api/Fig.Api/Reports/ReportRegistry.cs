namespace Fig.Api.Reports;

public class ReportRegistry : IReportRegistry
{
    private readonly IReadOnlyDictionary<string, IReport> _reports;

    public ReportRegistry(IEnumerable<IReport> reports)
    {
        _reports = reports.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IReport> GetAll()
        => _reports.Values.OrderBy(r => r.Category).ThenBy(r => r.Name).ToList();

    public IReport? Get(string reportId)
        => _reports.TryGetValue(reportId, out var report) ? report : null;
}
