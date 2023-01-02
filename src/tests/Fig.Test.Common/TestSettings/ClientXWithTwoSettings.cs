using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientXWithTwoSettings : SettingsBase
{
    public override string ClientName => "ClientX";

    [Setting("This is a single string", "Pig")]
    public string SingleStringSetting { get; set; }

    [Setting("This is an int default 4", 4)]
    public int FavouriteNumber { get; set; }
}