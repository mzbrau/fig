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
    public override string ClientSecret => "d4d2d3e5-0ba1-4b99-8aac-a53af64c75af";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}