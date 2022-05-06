using Fig.Client;
using Fig.Client.Attributes;
using Fig.Contracts.SettingVerification;
using Fig.Integration.Test.Api.TestSettings.Verifications;

namespace Fig.Integration.Test.Api.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
[Verification("WebsiteVerifier", "VerifiesWebsites", typeof(WebsiteVerifier), TargetRuntime.Dotnet6)]
public class SettingsWithVerifications : SettingsBase
{
    public override string ClientName => "SettingsWithVerifications";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}