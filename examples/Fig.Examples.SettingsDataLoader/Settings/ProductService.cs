using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Examples.SettingsDataLoader.Settings;

[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class ProductService : SettingsBase
{
    public override string ClientDescription => "Sample Product Service";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}