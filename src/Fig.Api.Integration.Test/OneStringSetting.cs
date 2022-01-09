using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Api.Integration.Test;

public class OneStringSetting : SettingsBase
{
    public override string ClientName => "OneStringSetting";

    public override string ClientSecret => "Secret456";
    
    [Setting("This is a single string", "Pig")]
    public string SingleStringSetting { get; set; }
}