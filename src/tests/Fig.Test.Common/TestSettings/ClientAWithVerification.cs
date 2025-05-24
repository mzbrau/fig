using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class ClientAWithVerification : TestSettingsBase
{
    public override string ClientName => "ClientA";
    public override string ClientDescription => "Client A with verification";

    [Setting("This is the address of a website")]
    public string WebsiteAddress { get; set; } = "http://www.google.com";

    [Setting("This is and IP Address")] 
    public string AnotherAddress { get; set; } = "8.8.8.8";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}