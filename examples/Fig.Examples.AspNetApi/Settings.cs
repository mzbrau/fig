using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.AspNetApi;

public class Settings : SettingsBase
{
    public override string ClientDescription => "AspNetApi Example";

    [Setting("The name of the city to get weather for.", "Melbourne")]
    public string? Location { get; set; }

    [Setting("Another setting in a different configuration section", "Windows")]
    [ConfigurationSectionOverride("OtherSettings")]
    public string MyOtherSetting { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}