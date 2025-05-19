using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class SettingsWithConfigError : TestSettingsBase
{
    public override string ClientName => "SettingsWithConfigError";
    public override string ClientDescription => "Client with config error";

    [Setting("This is a string")] 
    public string AStringSetting { get; set; } = "Horse";
    
    public override IEnumerable<string> GetValidationErrors()
    {
        return ["A config error"];
    }
}