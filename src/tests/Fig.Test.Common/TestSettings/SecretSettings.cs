using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class SecretSettings : TestSettingsBase
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

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}