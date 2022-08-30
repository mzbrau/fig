using Serilog.Core;
using Serilog.Events;

namespace Fig.Integration.SqlLookupTableService;

public interface ISettings
{
    string? FigUri { get; }
    
    string? FigUsername { get; }
    
    string? FigPassword { get; }

    int RefreshIntervalSeconds { get; }
    
    string? DatabaseConnectionString { get; }

    string? ConnectionStringPassword { get; }
    
    List<LookupTableConfiguration> Configuration { get; }
    
    LogEventLevel LogLevel { get; set; }

    bool AreValid(ILogger logger);
}