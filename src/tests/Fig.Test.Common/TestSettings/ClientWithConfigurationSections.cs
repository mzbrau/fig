

using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientWithConfigurationSections : TestSettingsBase
{
    public override string ClientName => "ClientWithConfigurationSections";
    public override string ClientDescription => "ClientWithConfigurationSections";

    [Setting("SerilogOverrideMicrosoft")]
    [ConfigurationSectionOverride("Serilog:Override", "Microsoft")]
    public string SerilogOverrideMicrosoft { get; set; } = "SerilogOverrideMicrosoftValue";
    
    [Setting("SerilogOverrideGoogle")]
    [ConfigurationSectionOverride("Serilog:Override", "Google")]
    public string SerilogOverrideGoogle { get; set; } = "SerilogOverrideGoogleValue";
    
    [Setting("SerilogOverrideValueGoogle")]
    [ConfigurationSectionOverride("Serilog:Override:Value", "Google")]
    public string SerilogOverrideValueGoogle { get; set; } = "SerilogOverrideValueGoogleValue";
    
    [Setting("SerilogOverrideAmazon")]
    [ConfigurationSectionOverride("Serilog:Override")]
    public string Amazon { get; set; } = "SerilogOverrideAmazonValue";
    
    [Setting("SerilogStuff")]
    [ConfigurationSectionOverride("Serilog")]
    public string Stuff { get; set; } = "SerilogStuffValue";
    
    [Setting("BaseConfig")]
    public string BaseConfig { get; set; } = "BaseConfigValue";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}