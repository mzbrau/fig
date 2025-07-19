using Fig.Client.Attributes;
using Fig.Client.Enums;
using Fig.Client.Validation;
using Serilog.Events;
using SettingsBase = Fig.Client.SettingsBase;

namespace Fig.Examples.AspNetApi;

public class Settings : SettingsBase
{
    public override string ClientDescription => "AspNetApi Example";

    [Setting("Override for system logs")]
    [ConfigurationSectionOverride("Serilog:Override", "System")]
    [ValidValues(typeof(LogEventLevel))]
    [Indent]
    public LogEventLevel SystemLogOverride { get; set; } = LogEventLevel.Warning;

    [Indent(2)]
    [Setting("The minimum log level")]
    [ConfigurationSectionOverride("Serilog:MinimumLevel", "Default")]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel MinLogLevel { get; set; } = LogEventLevel.Information;

    [Indent(3)]
    [Setting("The name of the section to write to")]
    [ConfigurationSectionOverride("Serilog:WriteTo", "Name")]
    public string WriteToName { get; set; } = "Console";
    
    [Setting("Override for microsoft logs")]
    [ConfigurationSectionOverride("Serilog:Override", "Microsoft")]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel MicrosoftLogOverride { get; set; } = LogEventLevel.Information;
    
    [Setting("The issue type")]
    [LookupTable(IssueTypeProvider.LookupNameKey, LookupSource.ProviderDefined)]
    [DependsOn(nameof(MicrosoftLogOverride), LogEventLevel.Warning)]
    public string? IssueType { get; set; }
    
    [Setting("The issue property name")]
    [LookupTable(IssuePropertyProvider.LookupNameKey, LookupSource.ProviderDefined, nameof(IssueType))]
    [DependsOn(nameof(MicrosoftLogOverride), LogEventLevel.Warning)]
    public string? IssuePropertyName { get; set; }
    
    [Setting("The name of the city to get weather for.")]
    [Validation(ValidationType.NotEmpty)]
    [DependsOn(nameof(MicrosoftLogOverride), LogEventLevel.Debug)]
    public string? Location { get; set; } = "Melbourne";
    
    public override IEnumerable<string> GetValidationErrors()
    {
        //Perform validation here.
        return [];
    }
}
