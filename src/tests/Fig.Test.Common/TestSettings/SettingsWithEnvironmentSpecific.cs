using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class SettingsWithEnvironmentSpecific : TestSettingsBase
{
    public override string ClientDescription => "Settings with Environment Specific attributes";

    public override string ClientName => "SettingsWithEnvironmentSpecific";

    [Setting("Regular Setting")]
    public string RegularSetting { get; set; } = "Regular";

    [Setting("Environment Specific Setting")]
    [EnvironmentSpecific]
    public string EnvironmentSpecificSetting { get; set; } = "Environment";

    [Setting("Another Regular Setting")]
    public int AnotherRegularSetting { get; set; } = 42;

    [Setting("Another Environment Specific Setting")]
    [EnvironmentSpecific]
    public bool AnotherEnvironmentSpecificSetting { get; set; } = true;

    [Setting("Secret Environment Specific Setting")]
    [EnvironmentSpecific]
    [Secret]
    public string SecretEnvironmentSpecificSetting { get; set; } = "SecretEnv";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}
