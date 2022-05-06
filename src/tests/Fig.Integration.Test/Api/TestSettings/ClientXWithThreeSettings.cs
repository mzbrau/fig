using System;
using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Integration.Test.Api.TestSettings;

public class ClientXWithThreeSettings : SettingsBase
{
    public override string ClientName => "ClientX";

    [Setting("This is a single string updated", "Pig")]
    public string SingleStringSetting { get; set; }

    [Setting("True if cool", true)] 
    public bool IsCool { get; set; }

    [Setting("The date of birth")] 
    public DateTime? DateOfBirth { get; set; }
}