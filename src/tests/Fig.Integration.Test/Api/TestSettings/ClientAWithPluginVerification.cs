using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Integration.Test.Api.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class ClientAWithPluginVerification : SettingsBase
{
    public override string ClientName => "ClientA";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
    
    [Setting("This is and IP Address", "8.8.8.8")]
    public string AnotherAddress { get; set; }
}