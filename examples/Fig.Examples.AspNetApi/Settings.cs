using Fig.Client;
using Fig.Client.Attributes;
using Serilog.Events;

namespace Fig.Examples.AspNetApi;

public class Settings : SettingsBase
{
    public override string ClientDescription => "AspNetApi Example";

    [Setting("The name of the city to get weather for.", "Melbourne")]
    public string? Location { get; set; }

    [Setting("The minimum log level", "Information")]
    [ConfigurationSectionOverride("Serilog:MinimumLevel", "Default")]
    [ValidValues(typeof(LogEventLevel))]
    public string MinLogLevel { get; set; }

    [Setting("Override for microsoft logs", "Warning")]
    [ConfigurationSectionOverride("Serilog:Override", "Microsoft")]
    [ValidValues(typeof(LogEventLevel))]
    public string MicrosoftLogOverride { get; set; }

    [Setting("Override for system logs", "Warning")]
    [ConfigurationSectionOverride("Serilog:Override", "System")]
    [ValidValues(typeof(LogEventLevel))]
    public string SystemLogOverride { get; set; }

    [Setting("The name of the section to write to", "Console")]
    [ConfigurationSectionOverride("Serilog:WriteTo", "Name")]
    public string WriteToName { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}
