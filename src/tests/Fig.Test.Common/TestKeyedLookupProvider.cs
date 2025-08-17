using Fig.Client.Abstractions.LookupTable;
using Fig.Client.LookupTable;

namespace Fig.Test.Common;

public class TestKeyedLookupProvider : IKeyedLookupProvider
{
    public const string LookupNameKey = "TestKeyedProviderLookup";
    
    public string LookupName => LookupNameKey;
    
    public Task<Dictionary<string, Dictionary<string, string?>>> GetItems()
    {
        return Task.FromResult(new Dictionary<string, Dictionary<string, string?>>
        {
            {
                "Category1", new Dictionary<string, string?>
                {
                    { "Item1A", "Category 1 - Item A" },
                    { "Item1B", "Category 1 - Item B" },
                    { "Item1C", null }
                }
            },
            {
                "Category2", new Dictionary<string, string?>
                {
                    { "Item2A", "Category 2 - Item A" },
                    { "Item2B", "Category 2 - Item B" },
                    { "Item2C", null }
                }
            },
            {
                "Category3", new Dictionary<string, string?>
                {
                    { "Item3A", "Category 3 - Item A" },
                    { "Item3B", null },
                    { "Item3C", "Category 3 - Item C" }
                }
            }
        });
    }
}