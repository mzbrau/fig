using Fig.Client;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class DiscountService : SettingsBase
{
    public override string ClientDescription => "Sample Discount Service";

    [Setting("This is a string")]
    [Validation("[0-9a-zA-Z]{5,}", "Must have 5 or more characters")]
    public string AStringSetting { get; set; } = "Horse";

    [Group("GroupA")]
    [Setting("This is an int")]
    public int IntSetting { get; set; } = 6;

    [Group("GroupA")]
    [Setting("This is a bool setting", true)]
    public bool ABoolSetting { get; set; }

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}