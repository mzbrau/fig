using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class InvalidSettings : SettingsBase
{
    [Setting("Some Test", true)]
    public bool TestSetting { get; set; }

    public override string ClientName => "Invalid*.Name";
    public override string ClientDescription => "Desc";
}