using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
[Verification("PingVerifier", nameof(AnotherAddress))]
public class ClientAWith2Verifications : TestSettingsBase
{
    public override string ClientName => "ClientA";
    public override string ClientDescription => "ClientA";


    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
    
    [Setting("This is and IP Address", "127.0.0.1")]
    public string AnotherAddress { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}