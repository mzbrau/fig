using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class ClientXWithTwoSettings : TestSettingsBase
{
    public override string ClientName => "ClientX";
    public override string ClientDescription => "Client with 2 settings";

    [Setting("This is a single string", "Pig")]
    public string SingleStringSetting { get; set; }

    [Setting("This is an int default 4", 4)]
    public int FavouriteNumber { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}