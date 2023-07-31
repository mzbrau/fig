using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
[Verification("PingVerifier", nameof(AnotherAddress))]
public class ClientAWith2PluginVerifications : SettingsBase
{
    public override string ClientName => "ClientA";
    public override string ClientDescription => "ClientA";


    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
    
    [Setting("This is and IP Address", "8.8.8.8")]
    public string AnotherAddress { get; set; }
}