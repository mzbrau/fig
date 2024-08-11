using System.Diagnostics;

namespace Fig.Api.Observability;

public static class ApiActivitySource
{
    public static string Name { get; } = "Fig.Api";
    
    public static ActivitySource Instance = new(Name, "1.0.0");
}