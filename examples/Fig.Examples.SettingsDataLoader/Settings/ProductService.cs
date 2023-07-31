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
    public override string ClientDescription => "Sample Product Service";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}