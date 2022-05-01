using Fig.Client;
using Fig.Client.Attributes;
using Fig.Contracts.SettingVerification;
using Fig.Examples.SettingsDataLoader.Verifications;

namespace Fig.Examples.SettingsDataLoader.Settings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
[Verification("WebsiteVerifier", "VerifiesWebsites", typeof(WebsiteVerifier), TargetRuntime.Dotnet6, nameof(WebsiteAddress))]
public class ProductService : SettingsBase
{
    public override string ClientName => "ProductService";
    public override string ClientSecret => "d4d2d3e5-0ba1-4b99-8aac-a53af64c75af";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}