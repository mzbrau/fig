using Fig.Client;
using Fig.Client.Attributes;
using Fig.Contracts.SettingVerification;
using Fig.Test.Common.TestSettings.Verifications;

namespace Fig.Test.Common.TestSettings;

[Verification("WebsiteVerifier", "VerifiesWebsites", typeof(WebsiteVerifier), TargetRuntime.Dotnet6)]
public class ClientAWithDynamicVerification : SettingsBase
{
    public override string ClientName => "ClientA";
    public override string ClientDescription => "Client A with dynamic verification";


    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
    
    [Setting("This is and IP Address", "8.8.8.8")]
    public string AnotherAddress { get; set; }
}