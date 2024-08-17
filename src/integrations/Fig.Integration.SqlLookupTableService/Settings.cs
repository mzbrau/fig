using Fig.Client;
using Fig.Client.Attributes;
using Serilog.Events;

namespace Fig.Integration.SqlLookupTableService;

public class Settings : SettingsBase
{
    public override string ClientDescription => "$Fig.Integration.SqlLookupTableService.ServiceDescription.md";

    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigUri")]
    public string? FigUri { get; set; } = "https://localhost:7281";
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigUsername")]
    public string? FigUsername { get; set; }
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigPassword")]
    [Secret]
    public string? FigPassword { get; set; }

    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#RefreshIntervalMs")]
    public int RefreshIntervalMs { get; set; } = 600000;
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#DatabaseConnectionString")]
    public string? DatabaseConnectionString { get; set; }
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#ConnectionStringPassword")]
    [Secret]
    public string? ConnectionStringPassword { get; set; }

    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#Configuration")]
    public List<LookupTableConfiguration>? Configuration { get; set; }

    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#LogLevel")]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    public override void Validate(ILogger logger)
    {
        var validationErrors = GetValidationErrors().ToList();
        
        if (validationErrors.Any() && !HasConfigurationError)
        {
            SetConfigurationErrorStatus(true, validationErrors);
            foreach (var error in validationErrors)
                logger.LogError(error);
        }

        if (!validationErrors.Any())
        {
            SetConfigurationErrorStatus(false);
        }
    }

    private IEnumerable<string> GetValidationErrors()
    {
        if (string.IsNullOrWhiteSpace(FigUsername))
            yield return "Username is not set";

        if (string.IsNullOrWhiteSpace(FigPassword))
            yield return "Password is not set";

        if (string.IsNullOrWhiteSpace(FigUri))
            yield return "Fig URI not set";

        if (string.IsNullOrWhiteSpace(DatabaseConnectionString))
            yield return "Database connection string not set";

        if (string.IsNullOrWhiteSpace(ConnectionStringPassword) && DatabaseConnectionString?.Contains("{0}") == true)
            yield return "Database connection string password not set";

        if (Configuration?.Any() != true)
            yield return "No lookup configurations set";
    }
}