namespace Fig.Contracts.Reports;

public class ReportParameterDataContract
{
    public ReportParameterDataContract(
        string name,
        string displayName,
        ReportParameterType type,
        bool required,
        object? defaultValue,
        ReportParameterLookupKind lookupKind)
    {
        Name = name;
        DisplayName = displayName;
        Type = type;
        Required = required;
        DefaultValue = defaultValue;
        LookupKind = lookupKind;
    }

    public string Name { get; }

    public string DisplayName { get; }

    public ReportParameterType Type { get; }

    public bool Required { get; }

    public object? DefaultValue { get; }

    public ReportParameterLookupKind LookupKind { get; }
}
