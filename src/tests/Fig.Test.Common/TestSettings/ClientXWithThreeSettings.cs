using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class ClientXWithThreeSettings : TestSettingsBase
{
    public override string ClientName => "ClientX";
    public override string ClientDescription => "Client with 3 settings";

    [Setting("This is a single string updated")]
    public string SingleStringSetting { get; set; } = "Pig";

    [Setting("True if cool")] 
    public bool IsCool { get; set; } = true;

    [Setting("The date of birth")] 
    public DateTime? DateOfBirth { get; set; }

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}