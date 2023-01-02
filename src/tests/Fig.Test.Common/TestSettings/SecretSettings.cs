using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class SecretSettings : SettingsBase
{
    public override string ClientName => "SecretClient";

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
    public int? SecretInt { get; set; }

}