using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ThreeSettings : SettingsBase
{
    public override string ClientName => "ThreeSettings";

    [Setting("This is a string", "Horse")]
    public string AStringSetting { get; set; }

    [Setting("This is an int", 6)]
    public int AnIntSetting { get; set; }

    [Setting("This is a bool setting", true)]
    public bool ABoolSetting { get; set; }
}