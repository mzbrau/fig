using Fig.Client.Attributes;

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

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}