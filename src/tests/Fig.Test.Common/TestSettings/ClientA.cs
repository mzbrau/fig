using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class ClientA : TestSettingsBase
{
    public override string ClientName => "ClientA";
    public override string ClientDescription => "ClientA";


    [Setting("This is the address of a website")]
    public string WebsiteAddress { get; set; } = "http://www.google.com";

    [Setting("This is the address of a website")]
    public string AnotherAddress { get; set; } = "http://www.google.com";

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}