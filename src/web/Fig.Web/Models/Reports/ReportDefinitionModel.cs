using Fig.Contracts.Reports;

namespace Fig.Web.Models.Reports;

public class ReportDefinitionModel
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<ReportParameterModel> Parameters { get; set; } = new();

    public string DisplayLabel => string.IsNullOrWhiteSpace(Category) ? Name : $"{Category} / {Name}";
}

public class ReportParameterModel
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ReportParameterType Type { get; set; }

    public bool Required { get; set; }

    public ReportParameterLookupKind LookupKind { get; set; }

    public object? Value { get; set; }
}
