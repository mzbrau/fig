using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Api.Integration.Test.TestSettings;

public class SecretSettings : SettingsBase
{
    public override string ClientName => "SecretClient";
    public override string ClientSecret => "96d04082-c87a-4257-9b8d-aed1a54156f6";
    
    [Setting("Not a secret")] 
    public string? NoSecret { get; set; }
    
    [Secret]
    [Setting("Secret with default", "cat")] 
    public string? SecretWithDefault { get; set; }
    
    [Secret]
    [Setting("Secret no default")] 
    public string? SecretNoDefault { get; set; }
    
    [Secret]
    [Setting("Secret int")] 
    public int SecretInt { get; set; }
    
}