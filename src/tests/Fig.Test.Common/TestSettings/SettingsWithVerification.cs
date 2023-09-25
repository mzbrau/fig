using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class SettingsWithVerification : SettingsBase
{
    public override string ClientName => "SettingsWithVerifications";
    public override string ClientDescription => "Settings with verifications";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}