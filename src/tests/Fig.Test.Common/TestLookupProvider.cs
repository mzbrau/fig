using Fig.Client.Abstractions.LookupTable;

namespace Fig.Test.Common;

public class TestLookupProvider : ILookupProvider
{
    public const string LookupNameKey = "TestProviderLookup";
    
    public string LookupName => LookupNameKey;
    
    public Task<Dictionary<string, string?>> GetItems()
    {
        return Task.FromResult(new Dictionary<string, string?>
        {
            { "Option1", "First Option" },
            { "Option2", "Second Option" },
            { "Option3", "Third Option" },
            { "NoAlias", null }
        });
    }
}