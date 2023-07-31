using Fig.Client;
using Fig.Client.Attributes;
using Serilog.Events;

namespace Fig.Integration.SqlLookupTableService;

public class Settings : SettingsBase, ISettings
{
    public override string ClientName => "SQL Lookup Table Service";
    public override string ClientDescription => "$Fig.Integration.SqlLookupTableService.ServiceDescription.md";

    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigUri", "https://localhost:7281")]
    [DisplayOrder(1)]
    public string? FigUri { get; set; }
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigUsername")]
    [DisplayOrder(2)]
    public string? FigUsername { get; set; }
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigPassword")]
    [DisplayOrder(3)]
    [Secret]
    public string? FigPassword { get; set; }

    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#RefreshIntervalSeconds", 600)]
    [DisplayOrder(4)]
    public int RefreshIntervalSeconds { get; set; }
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#DatabaseConnectionString")]
    [DisplayOrder(4)]
    public string? DatabaseConnectionString { get; set; }
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#ConnectionStringPassword")]
    [DisplayOrder(5)]
    [Secret]
    public string? ConnectionStringPassword { get; set; }
    
    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#Configuration")]
    [DisplayOrder(6)]
    public List<LookupTableConfiguration>? Configuration { get; set; }

    [Setting("$Fig.Integration.SqlLookupTableService.ServiceDescription.md#LogLevel", LogEventLevel.Information)]
    [DisplayOrder(7)]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel LogLevel { get; set; }

    public bool AreValid(ILogger logger)
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

        return !HasConfigurationError;
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