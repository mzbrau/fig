using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

[Verification("PingVerifier", nameof(AnotherAddress))]
public class ClientAWithVerification2 : TestSettingsBase
{
    public override string ClientName => "ClientA";
    public override string ClientDescription => "Client A with verification";

    [Setting("This is the address of a website")]
    public string WebsiteAddress { get; set; } = "http://www.google.com";

    [Setting("This is and IP Address")] 
    public string AnotherAddress { get; set; } = "8.8.8.8";

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}