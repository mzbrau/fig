using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.AspNetApi;

public class Settings : SettingsBase, ISettings
{
    public override string ClientName => "AspNetApi";

    [Setting("The name of the city to get weather for.", "Melbourne")]
    public string? Location { get; set; }
}