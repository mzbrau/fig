using Fig.Client;

namespace Fig.Test.Common.TestSettings;

public class NoSettings : SettingsBase
{
    public override string ClientName => "NoSettings";
    public override string ClientDescription => "Client with no settings";
}