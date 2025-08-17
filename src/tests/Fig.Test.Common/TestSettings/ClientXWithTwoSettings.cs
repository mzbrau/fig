using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientXWithTwoSettings : TestSettingsBase
{
    public override string ClientName => "ClientX";
    public override string ClientDescription => "Client with 2 settings";

    [Setting("This is a single string")] 
    public string SingleStringSetting { get; set; } = "Pig";

    [Setting("This is an int default 4")]
    public int FavouriteNumber { get; set; } = 4;

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}