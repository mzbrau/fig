using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class ClientBString : TestSettingsBase
{
    public override string ClientName => "ClientB";
    public override string ClientDescription => "ClientB";
    
    [Setting("Animals")]
    public string Animals { get; set; } = "Dog, Cat, Bird";
    
    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}