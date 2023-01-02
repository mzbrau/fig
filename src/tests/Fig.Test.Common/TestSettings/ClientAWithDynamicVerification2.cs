using Fig.Client;
using Fig.Client.Attributes;
using Fig.Contracts.SettingVerification;
using Fig.Test.Common.TestSettings.Verifications;

namespace Fig.Test.Common.TestSettings;

[Verification("WebsiteVerifier", "VerifiesWebsites v2", typeof(WebsiteVerifierV2), TargetRuntime.Dotnet6)]
public class ClientAWithDynamicVerification2 : SettingsBase
{
    public override string ClientName => "ClientA";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
    
    [Setting("This is and IP Address", "8.8.8.8")]
    public string AnotherAddress { get; set; }
}