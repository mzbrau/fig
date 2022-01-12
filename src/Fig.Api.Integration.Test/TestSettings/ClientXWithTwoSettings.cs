using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Api.Integration.Test.TestSettings;

public class ClientXWithTwoSettings : SettingsBase
{
    public override string ClientName => "ClientX";

    public override string ClientSecret => "a7a57ce5-5dae-4c35-920c-e13c1459e2a8";

    [Setting("This is a single string", "Pig")]
    public string SingleStringSetting { get; set; }

    [Setting("This is an int default 4", 4)]
    public int FavouriteNumber { get; set; }
}