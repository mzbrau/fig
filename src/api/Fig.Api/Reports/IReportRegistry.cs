namespace Fig.Api.Reports;

public interface IReportRegistry
{
    IReadOnlyList<IReport> GetAll();

    IReport? Get(string reportId);
}
