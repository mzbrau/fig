using Fig.Client.Abstractions.Attributes;

namespace Fig.Integration.SqlLookupTableService;

public class LookupTableConfiguration
{
    public string? Name { get; set; }
    
    [MultiLine(5)]
    public string? SqlExpression { get; set; }
}