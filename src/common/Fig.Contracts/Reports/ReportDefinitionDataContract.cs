using System.Collections.Generic;

namespace Fig.Contracts.Reports;

public class ReportDefinitionDataContract
{
    public ReportDefinitionDataContract(
        string id,
        string name,
        string category,
        string description,
        IList<ReportParameterDataContract> parameters,
        bool supportsAiAnalysis = false)
    {
        Id = id;
        Name = name;
        Category = category;
        Description = description;
        Parameters = parameters;
        SupportsAiAnalysis = supportsAiAnalysis;
    }

    public string Id { get; }

    public string Name { get; }

    public string Category { get; }

    public string Description { get; }

    public IList<ReportParameterDataContract> Parameters { get; }

    public bool SupportsAiAnalysis { get; }
}
