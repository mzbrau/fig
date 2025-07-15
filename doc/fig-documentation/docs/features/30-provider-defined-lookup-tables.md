---
sidebar_position: 30
sidebar_label: Provider Defined Lookup Tables
---

# Provider Defined Lookup Tables

Fig supports provider-defined lookup tables through the `ILookupProvider` and `IKeyedLookupProvider` interfaces. This feature allows applications to dynamically provide lookup table data at runtime, eliminating the need to manually create and maintain lookup tables in the Fig web interface.

:::tip

Provider-defined lookup tables are the preferred approach for dynamic lookup data. They automatically register when the implementing classes are registered with your application's dependency injection container and are referenced by settings with the `[LookupTable]` attribute.

:::

## ILookupProvider Interface

The `ILookupProvider` interface is used for simple lookup tables where the options are dynamic or only known at runtime.

### Implementation

```csharp
public interface ILookupProvider
{
    /// <summary>
    /// The name of the lookup table. This must match the name used in the LookupTable attribute on settings.
    /// </summary>
    string LookupName { get; }

    /// <summary>
    /// The key of the dictionary is the option value, and the value is an optional alias.
    /// </summary>
    /// <returns></returns>
    Task<Dictionary<string, string?>> GetItems();
}
```

### Example Implementation

```csharp
using Fig.Client.LookupTable;

public class IssueTypeProvider : ILookupProvider
{
    public const string LookupNameKey = "IssueType";
    
    public string LookupName => LookupNameKey;
    
    public Task<Dictionary<string, string?>> GetItems()
    {
        return Task.FromResult(new Dictionary<string, string?>
        {
            { "Bug", "🐛 Bug Report" },
            { "Feature", "✨ Feature Request" },
            { "Task", "📋 Task" },
            { "Documentation", "📚 Documentation" }
        });
    }
}
```

### Usage in Settings

```csharp
public class Settings : SettingsBase
{
    [Setting("The type of issue being tracked")]
    [LookupTable(IssueTypeProvider.LookupNameKey, LookupSource.ProviderDefined)]
    public string? IssueType { get; set; }
}
```

## IKeyedLookupProvider Interface

The `IKeyedLookupProvider` interface is used for lookup tables where the available options depend on the value of another setting. This creates a cascading dropdown effect.

### Interface Definition

```csharp
public interface IKeyedLookupProvider
{
    /// <summary>
    /// The name of the lookup table. This must match the name used in the LookupTable attribute on settings.
    /// </summary>
    string LookupName { get; }

    /// <summary>
    /// Gets the items for the lookup table.
    /// The key of the outer dictionary is the value of the other setting.
    /// The key of the inner dictionary is the option value, and the value is an optional alias.
    /// </summary>
    /// <returns></returns>
    Task<Dictionary<string, Dictionary<string, string?>>> GetItems();
}
```

### Keyed Lookup Example

```csharp
using Fig.Client.LookupTable;

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
                    { "Critical", "🔴 Critical Priority" },
                    { "High", "🟠 High Priority" },
                    { "Medium", "🟡 Medium Priority" },
                    { "Low", "🟢 Low Priority" }
                }
            },
            {
                "Feature", new Dictionary<string, string?>
                {
                    { "Open", "📭 Open" },
                    { "In Progress", "⚡ In Progress" },
                    { "Review", "👀 Under Review" },
                    { "Closed", "✅ Closed" }
                }
            },
            {
                "Task", new Dictionary<string, string?>
                {
                    { "Alice", "👩‍💻 Alice (Frontend)" },
                    { "Bob", "👨‍💻 Bob (Backend)" },
                    { "Charlie", "🧑‍💻 Charlie (DevOps)" }
                }
            }
        });
    }
}
```

### Usage in Settings with Dependency

```csharp
public class Settings : SettingsBase
{
    [Setting("The type of issue being tracked")]
    [LookupTable(IssueTypeProvider.LookupNameKey, LookupSource.ProviderDefined)]
    public string? IssueType { get; set; }
    
    [Setting("The specific property for this issue type")]
    [LookupTable(IssuePropertyProvider.LookupNameKey, LookupSource.ProviderDefined, nameof(IssueType))]
    public string? IssueProperty { get; set; }
}
```

## Registration and Automatic Discovery

Provider-defined lookup tables are automatically registered by the `FigLookupWorker<T>` when your application starts. The worker:

1. **Discovers Valid Lookup Tables**: Scans all properties in your settings class that have both `[Setting]` and `[LookupTable]` attributes
2. **Matches Providers**: Only registers providers whose `LookupName` matches a lookup table name used in your settings
3. **Registers with Fig**: Automatically calls the Fig API to register the lookup table data

### Dependency Injection Registration

Register your lookup providers with your dependency injection container:

```csharp
// In Program.cs or Startup.cs
services.AddSingleton<ILookupProvider, IssueTypeProvider>();
services.AddSingleton<IKeyedLookupProvider, IssuePropertyProvider>();

// Register Fig client which automatically includes the lookup worker
services.AddFig<MySettings>();
```

### Multiple Providers

You can register multiple providers of each type:

```csharp
services.AddSingleton<ILookupProvider, IssueTypeProvider>();
services.AddSingleton<ILookupProvider, DatabaseTableProvider>();
services.AddSingleton<ILookupProvider, ApiEndpointProvider>();

services.AddSingleton<IKeyedLookupProvider, IssuePropertyProvider>();
services.AddSingleton<IKeyedLookupProvider, TableColumnProvider>();
```

## Advanced Usage Examples

### Dynamic Data from External Sources

```csharp
public class DatabaseTableProvider : ILookupProvider
{
    private readonly IDbConnection _connection;
    
    public DatabaseTableProvider(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public string LookupName => "DatabaseTables";
    
    public async Task<Dictionary<string, string?>> GetItems()
    {
        var tables = await _connection.QueryAsync<(string Name, string Description)>(
            "SELECT table_name, table_comment FROM information_schema.tables WHERE table_schema = 'mydb'");
            
        return tables.ToDictionary(
            t => t.Name, 
            t => string.IsNullOrEmpty(t.Description) ? null : t.Description);
    }
}
```

### API-Based Lookup Data

```csharp
public class ServiceEndpointProvider : ILookupProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceEndpointProvider> _logger;
    
    public ServiceEndpointProvider(HttpClient httpClient, ILogger<ServiceEndpointProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public string LookupName => "ServiceEndpoints";
    
    public async Task<Dictionary<string, string?>> GetItems()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ServiceEndpoint[]>("/api/endpoints");
            return response?.ToDictionary(e => e.Url, e => e.Description) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load service endpoints for lookup table");
            return new Dictionary<string, string?>();
        }
    }
}

public record ServiceEndpoint(string Url, string Description);
```

## Benefits

- **Automatic Registration**: No manual lookup table creation required
- **Dynamic Data**: Lookup tables update automatically when your application restarts
- **Type Safety**: Lookup table names are enforced through constants
- **Dependency Support**: Create cascading dropdowns with `IKeyedLookupProvider`
- **External Integration**: Easily integrate with databases, APIs, or other external systems
- **Fault Tolerance**: Failed lookups don't prevent application startup
