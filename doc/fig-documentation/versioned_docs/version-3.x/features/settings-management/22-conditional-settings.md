---
sidebar_position: 22
---

# Conditional Settings (DependsOn attribute)

The `DependsOn` attribute provides a powerful way to create conditional visibility for settings based on the value of another property. This is particularly useful when certain settings are only relevant when another setting has specific values.

## Overview

When a setting has the `DependsOn` attribute:

- It will only be visible when the specified property has one of the defined valid values
- The setting's indentation level is automatically incremented by 1 to show the visual hierarchy
- A visual indicator shows on the controlling setting to indicate how many settings depend on it

:::note

Note that this in no way impacts the value of these settings. It will remain unchanged regardless of if the setting is visible or not.
However, generally it is expected that these settings will be ignored by the application if not relevant.

:::

## Usage

### Basic Example

```csharp
[Setting("Enable authentication")]
public bool UseAuthentication { get; set; } = false;

[Setting("Username for authentication")]
[DependsOn(nameof(UseAuthentication), true)]
public string? Username { get; set; }

[Setting("Password for authentication")]
[Secret]
[DependsOn(nameof(UseAuthentication), true)]
public string? Password { get; set; }
```

In this example:

- When `UseAuthentication` is `false`, both `Username` and `Password` settings are hidden
- When `UseAuthentication` is `true`, both `Username` and `Password` settings become visible
- Both dependent settings are automatically indented to show they belong to the authentication group

### Multiple Valid Values

You can specify multiple valid values that will make the dependent setting visible:

```csharp
[Setting("Connection type")]
[ValidValues("Database", "API", "File", "Memory")]
public string ConnectionType { get; set; } = "Memory";

[Setting("Database connection string")]
[DependsOn(nameof(ConnectionType), "Database")]
public string? DatabaseConnectionString { get; set; }

[Setting("API endpoint URL")]
[DependsOn(nameof(ConnectionType), "API")]
public string? ApiEndpoint { get; set; }

[Setting("File path")]
[DependsOn(nameof(ConnectionType), "File")]
public string? FilePath { get; set; }

[Setting("Cache configuration")]
[DependsOn(nameof(ConnectionType), "Database", "API")]
public bool EnableCaching { get; set; } = true;
```

In this example:

- `DatabaseConnectionString` is only visible when `ConnectionType` is "Database"
- `ApiEndpoint` is only visible when `ConnectionType` is "API"
- `FilePath` is only visible when `ConnectionType` is "File"
- `EnableCaching` is visible when `ConnectionType` is either "Database" or "API"

### Enum Dependencies

The attribute works seamlessly with enum properties:

```csharp
public enum LogLevel
{
    None,
    Error,
    Warning,
    Information,
    Debug
}

[Setting("Logging level")]
[ValidValues(typeof(LogLevel))]
public LogLevel LoggingLevel { get; set; } = LogLevel.Information;

[Setting("Log file path")]
[DependsOn(nameof(LoggingLevel), LogLevel.Error, LogLevel.Warning, LogLevel.Information, LogLevel.Debug)]
public string? LogFilePath { get; set; }

[Setting("Enable debug output")]
[DependsOn(nameof(LoggingLevel), LogLevel.Debug)]
public bool EnableDebugOutput { get; set; } = true;
```

## Parameters

- **dependsOnProperty** (string): The name of the property this setting depends on. Must be a valid property name within the same settings class.
- **validValues** (params object[]): One or more values that will make this setting visible. Values are automatically converted to strings for comparison.

## Validation

The `DependsOn` attribute includes built-in validation:

- The referenced property name must exist in the same settings class
- At least one valid value must be specified
- If the referenced property doesn't exist, an `InvalidSettingException` is thrown during registration

## Visual Indicators

### Automatic Indentation

Settings with the `DependsOn` attribute are automatically indented by one level (10px) to create a visual hierarchy, making it clear which settings are dependent on others.

### Dependency Indicator

The controlling setting (the one being depended upon) displays a small eye icon that shows how many settings depend on its value. Hovering over this icon reveals the exact count.

## Behavior

### Real-time Updates

When the controlling setting's value changes:

- Dependent settings are immediately shown or hidden based on the new value
- The UI updates in real-time without requiring a page refresh
- Hidden settings retain their values but are not visible to users

### Persistence

- Hidden settings maintain their configured values
- When a setting becomes visible again, it shows the previously configured value
- Values of hidden settings are still sent to the application
