using Fig.Client.Attributes;
using Serilog.Events;
using SettingsBase = Fig.Client.SettingsBase;

namespace Fig.Examples.AspNetApi;

public class Settings : SettingsBase
{
    public override string ClientDescription => "AspNetApi Example";

    [Setting("The name of the city to get weather for.")]
    public string? Location { get; set; } = "Melbourne";
    
    [Setting("Override for system logs")]
    [ConfigurationSectionOverride("Serilog:Override", "System")]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel SystemLogOverride { get; set; } = LogEventLevel.Warning;

    [Setting("The minimum log level")]
    [ConfigurationSectionOverride("Serilog:MinimumLevel", "Default")]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel MinLogLevel { get; set; } = LogEventLevel.Information;

    [Setting("The name of the section to write to")]
    [ConfigurationSectionOverride("Serilog:WriteTo", "Name")]
    public string WriteToName { get; set; } = "Console";
    
    [Setting("Override for microsoft logs")]
    [ConfigurationSectionOverride("Serilog:Override", "Microsoft")]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel MicrosoftLogOverride { get; set; } = LogEventLevel.Information;
    
    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}
