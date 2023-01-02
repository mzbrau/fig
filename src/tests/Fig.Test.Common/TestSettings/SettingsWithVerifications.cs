using Fig.Client;
using Fig.Client.Attributes;
using Fig.Contracts.SettingVerification;
using Fig.Test.Common.TestSettings.Verifications;

namespace Fig.Test.Common.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
[Verification("WebsiteVerifier", "VerifiesWebsites", typeof(WebsiteVerifier), TargetRuntime.Dotnet6)]
public class SettingsWithVerifications : SettingsBase
{
    public override string ClientName => "SettingsWithVerifications";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}