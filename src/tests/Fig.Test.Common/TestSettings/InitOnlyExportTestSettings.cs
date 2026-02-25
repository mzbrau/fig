using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

public class InitOnlyExportTestSettings : TestSettingsBase
{
    public override string ClientName => "InitOnlyExportTestSettings";
    public override string ClientDescription => "Test settings for InitOnlyExport functionality";

    [Setting("Bootstrap value")]
    [InitOnlyExport]
    public string BootstrapValue { get; set; } = "bootstrap";

    [Setting("Regular value")]
    public string RegularValue { get; set; } = "regular";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}
