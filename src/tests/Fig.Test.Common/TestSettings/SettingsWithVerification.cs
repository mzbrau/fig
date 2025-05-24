using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class SettingsWithVerification : TestSettingsBase
{
    public override string ClientName => "SettingsWithVerifications";
    public override string ClientDescription => "Settings with verifications";

    [Setting("This is the address of a website")]
    public string WebsiteAddress { get; set; } = "http://www.google.com";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}