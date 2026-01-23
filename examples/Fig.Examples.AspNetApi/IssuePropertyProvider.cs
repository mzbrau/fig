using Fig.Client.Abstractions.LookupTable;

namespace Fig.Examples.AspNetApi;

public class IssuePropertyProvider : IKeyedLookupProvider
{
    public const string LookupNameKey = "IssueProperty";
    
    public string LookupName => LookupNameKey;
    public Task<Dictionary<string, Dictionary<string, string?>>> GetItems()
    {
        return Task.FromResult(new Dictionary<string, Dictionary<string, string?>>
        {
            {
                "Bug", new Dictionary<string, string?>
                {
                    { "High", null },
                    { "Medium", null },
                    { "Low", null }
                }
            },
            {
                "Feature", new Dictionary<string, string?>
                {
                    { "Open", null },
                    { "In Progress", null },
                    { "Closed", null }
                }
            },
            {
                "Task", new Dictionary<string, string?>
                {
                    { "Alice", null },
                    { "Bob", null },
                    { "Charlie", null }
                }
            }
        });
    }
}