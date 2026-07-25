using System.Collections.Generic;

namespace Fig.Contracts.Reports;

public class ReportExecutionRequestDataContract
{
    public ReportExecutionRequestDataContract(
        Dictionary<string, object?> parameters,
        ReportFormat format = ReportFormat.Html,
        bool enableAiAnalysis = false,
        string? aiPrompt = null)
    {
        Parameters = parameters;
        Format = format;
        EnableAiAnalysis = enableAiAnalysis;
        AiPrompt = aiPrompt;
    }

    public Dictionary<string, object?> Parameters { get; }

    public ReportFormat Format { get; }

    public bool EnableAiAnalysis { get; }

    public string? AiPrompt { get; }
}
