using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class OrdersService : SettingsBase
{
    public override string ClientName => "OrdersService";


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
}