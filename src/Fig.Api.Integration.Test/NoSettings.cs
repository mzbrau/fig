using Fig.Client;

namespace Fig.Api.Integration.Test;

public class NoSettings : SettingsBase
{
    public override string ClientName => "NoSettings";

    public override string ClientSecret => "Secret123";
}