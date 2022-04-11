using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.AspNetApi;

public class Settings : SettingsBase, ISettings
{
    public override string ClientName => "AspNetApi";
    public override string ClientSecret => "b7c90e4740f943ffa71f5ac44f81f52b";
    
    [Setting("The name of the city to get weather for.", "Melbourne")]
    public string? Location { get; set; }
}