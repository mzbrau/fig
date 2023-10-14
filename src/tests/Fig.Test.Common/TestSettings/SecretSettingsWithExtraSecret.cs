using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class SecretSettingsWithExtraSecret : SettingsBase
{
    public override string ClientName => "SecretClient";
    public override string ClientDescription => "Client with secret settings;";

    [Setting("Not a secret")]
    public string? NoSecret { get; set; }

    [Secret]
    [Setting("Secret with default", "cat")]
    public string? SecretWithDefault { get; set; }

    [Secret]
    [Setting("Secret no default")]
    public string? SecretNoDefault { get; set; }
    
    [Secret]
    [Setting("Extra Secret", "dog")]
    public string? ExtraSecret { get; set; }
}