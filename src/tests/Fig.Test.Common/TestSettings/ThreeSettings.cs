using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class ThreeSettings : TestSettingsBase
{
    public override string ClientName => "ThreeSettings";
    public override string ClientDescription => "Client with 3 settings";

    [Setting("This is a string", "Horse")]
    public string AStringSetting { get; set; }

    [Setting("This is an int", 6, supportsLiveUpdate: false)]
    public int AnIntSetting { get; set; }

    [Setting("This is a bool setting", true)]
    public bool ABoolSetting { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}