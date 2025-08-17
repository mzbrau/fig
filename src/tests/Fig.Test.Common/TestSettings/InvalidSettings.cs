using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

public class InvalidSettings : TestSettingsBase
{
    [Setting("Some Test", true)]
    public bool TestSetting { get; set; }

    public override string ClientName => "Invalid*.Name";
    public override string ClientDescription => "Desc";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}