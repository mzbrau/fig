using Fig.Client;
using Fig.Client.Attributes;
using Serilog.Events;

namespace Fig.Integration.SqlLookupTableService;

public class Settings : SettingsBase, ISettings
{
    public override string ClientName => "SQL Lookup Table Service";
    
    [Setting("The base URI where the Fig API is listening", "https://localhost:7281")]
    [DisplayOrder(1)]
    public string? FigUri { get; set; }
    
    [Setting("The username when logging into Fig")]
    [DisplayOrder(2)]
    public string? FigUsername { get; set; }
    
    [Setting("The password corresponding to the username for Fig")]
    [DisplayOrder(3)]
    [Secret]
    public string? FigPassword { get; set; }

    [Setting("The interval in seconds between reading the database and updating Fig.", 600)]
    [DisplayOrder(4)]
    public int RefreshIntervalSeconds { get; set; }
    
    [Setting("The connection string for the SQL database")]
    [DisplayOrder(4)]
    public string? DatabaseConnectionString { get; set; }
    
    [Setting("The password component of the SQL connection string (if required). " +
             "Just add {0} in the connection string in the place where the password should be located.")]
    [DisplayOrder(5)]
    [Secret]
    public string? ConnectionStringPassword { get; set; }
    
    [Setting("The lookup table configurations. The name will be used for the lookup table name. " +
             "The SQL statement must return 2 values, the first will be the key and the second the value for the lookup table." +
             "The key must be unique.")]
    [DisplayOrder(6)]
    public List<LookupTableConfiguration>? Configuration { get; set; }

    [Setting("The minimum level at which to log", LogEventLevel.Information)]
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