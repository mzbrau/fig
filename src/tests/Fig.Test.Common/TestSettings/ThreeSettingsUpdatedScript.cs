using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class ThreeSettingsUpdatedScript : TestSettingsBase
{
    public const string DisplayScript =
        "if (ABoolSetting) { AnIntSetting.Visible = false } else { AnIntSetting.Visible = true }";
    
    public override string ClientName => "ThreeSettings";
    public override string ClientDescription => "Client with 3 settings";

    [Setting("This is a string")] 
    public string AStringSetting { get; set; } = "Horse";

    [Setting("This is an int", false)]
    public int AnIntSetting { get; set; } = 6;

    [Setting("This is a bool setting")]
    [DisplayScript(DisplayScript)]
    public bool ABoolSetting { get; set; } = true;

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}