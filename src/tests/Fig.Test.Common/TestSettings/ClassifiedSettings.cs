using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Data;

namespace Fig.Test.Common.TestSettings;

public class ClassifiedSettings : TestSettingsBase
{
    public override string ClientDescription => "Classified settings";

    public override string ClientName => "Classified Settings";
    
    [Setting("This is a technical setting")]
    public string TechnicalSetting { get; set; } = "Tech";
    
    [Setting("This is a functional setting", classification: Classification.Functional)]
    public string FunctionalSetting { get; set; } = "Func";
    
    [Setting("This is a Special setting", classification: Classification.Special)]
    public string SpecialSetting { get; set; } = "Special";
    
    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}