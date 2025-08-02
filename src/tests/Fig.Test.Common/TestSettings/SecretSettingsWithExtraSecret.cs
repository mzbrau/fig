using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class SecretSettingsWithExtraSecret : TestSettingsBase
{
    public override string ClientName => "SecretClient";
    public override string ClientDescription => "Client with secret settings;";

    [Setting("Not a secret")]
    public string? NoSecret { get; set; }

    [Secret]
    [Setting("Secret with default")]
    public string? SecretWithDefault { get; set; } = "cat";

    [Secret]
    [Setting("Secret no default")]
    public string? SecretNoDefault { get; set; }

    [Secret] [Setting("Extra Secret")] 
    public string? ExtraSecret { get; set; } = "dog";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}