using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
[Verification("PingVerifier", nameof(AnotherAddress))]
public class ClientAWith2Verifications : TestSettingsBase
{
    public override string ClientName => "ClientA";
    public override string ClientDescription => "ClientA";


    [Setting("This is the address of a website")]
    public string WebsiteAddress { get; set; } = "http://www.google.com";

    [Setting("This is and IP Address")] 
    public string AnotherAddress { get; set; } = "127.0.0.1";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}