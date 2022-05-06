using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class DiscountService : SettingsBase
{
    public override string ClientName => "DiscountService";

    [Setting("This is a string", "Horse")]
    [Validation("[0-9a-zA-Z]{5,}", "Must have 5 or more characters")]
    public string AStringSetting { get; set; }

    [Group("GroupA")]
    [Setting("This is an int", 6)]
    public int IntSetting { get; set; }

    [Group("GroupA")]
    [Setting("This is a bool setting", true)]
    public bool ABoolSetting { get; set; }
}