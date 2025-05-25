using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Integration.ConsoleWebHookHandler.Configuration;

public class Settings : SettingsBase
{
    public override string ClientDescription => "Web Hook Handler";

    [Setting("The hashed secret provided by fig when configuring the web hook client.")]
    public string HashedSecret { get; set; } = string.Empty;

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}