using Fig.Client.Abstractions.LookupTable;

namespace Fig.Examples.AspNetApi;

public class IssueTypeProvider : ILookupProvider
{
    public const string LookupNameKey = "IssueType";
    
    public string LookupName => LookupNameKey;
    public Task<Dictionary<string, string?>> GetItems()
    {
        return Task.FromResult(new Dictionary<string, string?>
        {
            { "Bug", null },
            { "Feature", null },
            { "Task", null }
        });
    }
}