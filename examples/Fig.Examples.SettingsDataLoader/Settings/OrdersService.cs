using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class OrdersService : SettingsBase
{
    public override string ClientDescription => "Sample Orders Service";


    [Setting("This is a single string updated")]
    public string SingleStringSetting { get; set; } = "Pig";

    [Group("GroupB")]
    [Setting("True if cool", true)]
    public bool IsCool { get; set; }

    [Setting("The date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Setting("This is an advanced setting, it is not normally changed")]
    [Advanced]
    public string? AdvancedSetting { get; set; } = "xx";
    
    [Setting("Setting with multi line string")]
    public List<MySettings>? MySettings { get; set; }

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}

public class MySettings
{
    public string Key { get; set; } = string.Empty;
    
    [MultiLine(4)]
    public string Value { get; set; } = string.Empty;
}