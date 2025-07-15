using Fig.Client.LookupTable;

namespace Fig.Test.Common;

public class TestCategoryLookupProvider : ILookupProvider
{
    public const string LookupNameKey = "TestProviderCategory";
    
    public string LookupName => LookupNameKey;
    
    public Task<Dictionary<string, string?>> GetItems()
    {
        return Task.FromResult(new Dictionary<string, string?>
        {
            { "Category1", null },
            { "Category2", null },
            { "Category3", null }
        });
    }
}
