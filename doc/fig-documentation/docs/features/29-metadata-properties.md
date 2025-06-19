---
sidebar_position: 29
sidebar_label: Metadata Properties
---

# Metadata Properties

Fig automatically tracks when your settings are loaded and updated, providing you with metadata about the state of your configuration. This feature is particularly useful for monitoring, debugging, and implementing health checks in your applications.

## Available Properties

All settings classes that inherit from `SettingsBase` automatically include two properties that provide information about the last update:

### LastFigUpdateUtc

A `DateTime?` property that indicates when the settings were last successfully loaded or updated.

```csharp
public class MySettings : SettingsBase
{
    public string DatabaseConnection { get; set; }
    public int RetryCount { get; set; }
    
    // Inherited from SettingsBase
    // public DateTime? LastFigUpdateUtc { get; }
}

// Usage
var settings = serviceProvider.GetRequiredService<IOptionsMonitor<MySettings>>();
var lastUpdate = settings.CurrentValue.LastFigUpdateUtc;

if (lastUpdate.HasValue)
{
    Console.WriteLine($"Settings last updated: {lastUpdate.Value:yyyy-MM-dd HH:mm:ss} UTC");
}
else
{
    Console.WriteLine("Settings have not been successfully loaded");
}
```

### FigSettingLoadType

An enum property that indicates how the settings were loaded. This helps you understand whether your application is running with the latest server configuration or fallback values.

```csharp
public enum LoadType
{
    None,    // Settings failed to load completely
    Server,  // Settings loaded from Fig API
    Offline  // Settings loaded from offline cache
}

// Usage
var settings = serviceProvider.GetRequiredService<IOptionsMonitor<MySettings>>();
var loadType = settings.CurrentValue.FigSettingLoadType;

switch (loadType)
{
    case LoadType.Server:
        Console.WriteLine("✓ Running with latest settings from Fig API");
        break;
    case LoadType.Offline:
        Console.WriteLine("⚠ Running with cached settings (API unavailable)");
        break;
    case LoadType.None:
        Console.WriteLine("❌ Running with default values (no settings loaded)");
        break;
}
```

## Common Use Cases

### Health Checks

Use these properties to implement health checks that verify your application has successfully loaded recent settings:

```csharp
public class SettingsHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<MySettings> _settings;
    private readonly TimeSpan _maxAge;

    public SettingsHealthCheck(IOptionsMonitor<MySettings> settings)
    {
        _settings = settings;
        _maxAge = TimeSpan.FromMinutes(30); // Consider settings stale after 30 minutes
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var currentSettings = _settings.CurrentValue;
        
        // Check if settings loaded successfully
        if (currentSettings.FigSettingLoadType == LoadType.None)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Fig settings failed to load"));
        }
        
        // Check if settings are recent
        if (currentSettings.LastFigUpdateUtc.HasValue)
        {
            var age = DateTime.UtcNow - currentSettings.LastFigUpdateUtc.Value;
            if (age > _maxAge)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Fig settings are {age.TotalMinutes:F1} minutes old"));
            }
        }
        
        var message = currentSettings.FigSettingLoadType == LoadType.Server 
            ? "Fig settings loaded from server" 
            : "Fig settings loaded from offline cache";
            
        return Task.FromResult(HealthCheckResult.Healthy(message));
    }
}
```

### Monitoring and Alerting

Monitor your application's configuration state:

```csharp
public class SettingsMonitor
{
    private readonly IOptionsMonitor<MySettings> _settings;
    private readonly ILogger<SettingsMonitor> _logger;

    public SettingsMonitor(IOptionsMonitor<MySettings> settings, ILogger<SettingsMonitor> logger)
    {
        _settings = settings;
        _logger = logger;
        
        // Monitor for settings changes
        _settings.OnChange(OnSettingsChanged);
    }

    private void OnSettingsChanged(MySettings settings)
    {
        var loadType = settings.FigSettingLoadType;
        var lastUpdate = settings.LastFigUpdateUtc;
        
        _logger.LogInformation("Settings updated - LoadType: {LoadType}, LastUpdate: {LastUpdate}", 
            loadType, lastUpdate);
            
        // Send metrics to your monitoring system
        if (loadType == LoadType.Offline)
        {
            _logger.LogWarning("Application running with offline settings cache");
        }
    }
}
```

### Conditional Logic Based on Settings Age

Implement different behavior based on how recently settings were updated:

```csharp
public class DataService
{
    private readonly IOptionsMonitor<MySettings> _settings;

    public async Task ProcessDataAsync()
    {
        var currentSettings = _settings.CurrentValue;
        
        // Use more conservative timeouts if settings are old or offline
        var timeout = GetTimeoutBasedOnSettingsAge(currentSettings);
        
        using var httpClient = new HttpClient { Timeout = timeout };
        // ... rest of implementation
    }

    private TimeSpan GetTimeoutBasedOnSettingsAge(MySettings settings)
    {
        // If settings failed to load, use conservative defaults
        if (settings.FigSettingLoadType == LoadType.None)
        {
            return TimeSpan.FromSeconds(30);
        }
        
        // If using offline settings, be more conservative
        if (settings.FigSettingLoadType == LoadType.Offline)
        {
            return TimeSpan.FromSeconds(settings.TimeoutSeconds + 10);
        }
        
        // If settings are very recent, use configured value
        if (settings.LastFigUpdateUtc.HasValue && 
            DateTime.UtcNow - settings.LastFigUpdateUtc.Value < TimeSpan.FromMinutes(5))
        {
            return TimeSpan.FromSeconds(settings.TimeoutSeconds);
        }
        
        // Default to slightly more conservative timeout
        return TimeSpan.FromSeconds(settings.TimeoutSeconds + 5);
    }
}
```

## Important Notes

- **UTC Timestamps**: `LastFigUpdateUtc` is always in UTC to avoid timezone confusion
- **Null Values**: `LastFigUpdateUtc` will be `null` if settings have never been successfully loaded
- **Automatic Updates**: These properties are automatically updated whenever Fig reloads your settings
- **Thread Safety**: These properties are updated atomically when settings change
- **Default Values**: When `LoadType.None`, your application runs with the default values defined in your settings class
