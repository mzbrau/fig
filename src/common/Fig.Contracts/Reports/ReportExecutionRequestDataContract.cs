using System.Collections.Generic;

namespace Fig.Contracts.Reports;

public class ReportExecutionRequestDataContract
{
    public ReportExecutionRequestDataContract(
        Dictionary<string, object?> parameters,
        ReportFormat format = ReportFormat.Html)
    {
        Parameters = parameters;
        Format = format;
    }

    public Dictionary<string, object?> Parameters { get; }

    public ReportFormat Format { get; }
}
