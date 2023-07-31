using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Integration.ConsoleWebHookHandler.Settings;

public class Settings : SettingsBase, ISettings
{
    public override string ClientName => "Console Web Hook Handler";
    public override string ClientDescription => "Web Hook Handler";

    [Setting("The hashed secret provided by fig when configuring the web hook client.", "")]
    public string HashedSecret { get; set; }

    
}