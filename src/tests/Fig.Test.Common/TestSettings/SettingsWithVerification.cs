using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class SettingsWithVerification : TestSettingsBase
{
    public override string ClientName => "SettingsWithVerifications";
    public override string ClientDescription => "Settings with verifications";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}