---
sidebar_position: 9
---

# Environment Specific

The `EnvironmentSpecificAttribute` is used to mark settings that are likely specific to a particular environment. These settings can be excluded from value-only exports to make it easier to transfer configurations between different environments (development, staging, production, etc.).

## Overview

When deploying applications across multiple environments, certain settings are typically environment-specific and should not be copied between environments. Examples include:

- Database connection strings
- API endpoints
- File paths
- Environment-specific feature flags
- Logging configurations
- Service URLs

By marking these settings with the `EnvironmentSpecificAttribute`, you can export configurations without accidentally overriding environment-specific values when importing to a different environment.

## Usage

### Marking Settings as Environment Specific

To mark a setting as environment-specific, apply the `[EnvironmentSpecific]` attribute to the property:

```csharp
using Fig.Client.Attributes;

public class MySettings : SettingsBase
{
    [Setting("Application Name")]
    public string ApplicationName { get; set; } = "MyApp";

    [Setting("Database Connection String")]
    [EnvironmentSpecific]
    public string DatabaseConnectionString { get; set; } = "Server=localhost;Database=MyDb";

    [Setting("API Endpoint URL")]
    [EnvironmentSpecific]
    public string ApiEndpoint { get; set; } = "https://localhost:5000/api";

    [Setting("Feature Flag - Enable Logging")]
    public bool EnableLogging { get; set; } = true;

    [Setting("Log File Path")]
    [EnvironmentSpecific]
    public string LogFilePath { get; set; } = "/var/log/myapp.log";
}
```

## Value-Only Export with Environment Filtering

When performing value-only exports, you can choose to exclude environment-specific settings by enabling the "Exclude Environment Specific Settings" option.

### Web UI Export

1. Navigate to the Import/Export page
2. In the "Value Only Export" section
3. Toggle on "Exclude Environment Specific Settings"
4. Click "Export"

The exported JSON will only contain settings that are **not** marked with `[EnvironmentSpecific]`.

## Import Behavior

When importing value-only exports that exclude environment-specific settings:

- **Regular settings** in the import will update existing values
- **Environment-specific settings** on the target environment remain unchanged
- This allows you to update application logic settings without affecting environment configuration

### Example Workflow

1. **Development Environment:** Configure all settings including environment-specific ones
2. **Export:** Create value-only export with "Exclude Environment Specific Settings" enabled  
3. **Staging/Production:** Import the export file
4. **Result:** Application logic settings are updated, but database connections, API endpoints, etc. remain specific to each environment

## Best Practices

### When to Use EnvironmentSpecific

Mark settings as environment-specific when they:

- Contain environment-specific infrastructure details (URLs, paths, connection strings)
- Have different values across environments by design
- Should not be accidentally overwritten during configuration deployments
- Represent environment-specific feature toggles

### When NOT to Use EnvironmentSpecific

Avoid marking settings as environment-specific when they:

- Represent business logic that should be consistent across environments
- Are application behavior settings that should sync between environments
- Are user preferences or feature configurations
- Need to be updated consistently across all environments
