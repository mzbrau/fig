using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class OrdersService : SettingsBase
{
    public override string ClientName => "OrdersService";
    public override string ClientDescription => "Sample Orders Service";


    [Setting("This is a single string updated", "Pig")]
    public string SingleStringSetting { get; set; }

    [Group("GroupB")]
    [Setting("True if cool", true)]
    public bool IsCool { get; set; }

    [Setting("The date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Setting("This is an advanced setting, it is not normally changed", "xx")]
    [Advanced]
    public string? AdvancedSetting { get; set; }
    
    [Setting("Setting with multi line string")]
    public List<MySettings> MySettings { get; set; }
}

public class MySettings
{
    public string Key { get; set; }
    
    [MultiLine(4)]
    public string Value { get; set; }
}