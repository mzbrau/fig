
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class SettingsWithJson : TestSettingsBase
{
    public override string ClientDescription => "TestSettingsWithJson";

    public override string ClientName => "TestSettingsWithJson";
    
    [Setting("Pet")]
    public Pet? Pet { get; set; }
    
    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}

public class Pet
{
    public required string Name { get; set; }
}