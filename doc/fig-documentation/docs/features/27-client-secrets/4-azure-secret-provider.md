---
sidebar_position: 4
sidebar_label: Azure Client Secret Provider
---

# Fig Azure Client Secret Provider

:::warning[Experimental]

This integration has not been well tested and may contain bugs. Please report any bugs to the github repo

:::

This package provides an Azure Key Vault implementation of the `IClientSecretProvider` interface for Fig configuration management.

## Features

- **Thread-Safe**: Uses semaphore-based locking to prevent race conditions when multiple instances try to create the same secret
- **Race Condition Protection**: Implements "check-then-act" pattern with proper synchronization
- **Retry Logic**: Handles transient failures with exponential backoff
- **Conflict Resolution**: Gracefully handles concurrent secret creation attempts
- **Managed Identity Support**: Uses `DefaultAzureCredential` for seamless authentication in Azure environments
- **Automatic Secret Creation**: Secrets are only auto-created if the environment variable `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is set to `Development`. In all other environments, secrets must already exist or a `SecretNotFoundException` will be thrown.

## Usage


```csharp
var secretProvider = new AzureSecretProvider("https://your-keyvault.vault.azure.net/");

builder.Configuration.SetBasePath(GetBasePath())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "AspNetApi";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        options.ClientSecretProvider = secretProvider
    });
```

## Auto-Creation Behavior

Secrets are only auto-created if the environment variable `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is set to `Development`. In all other environments, secrets must already exist or a `SecretNotFoundException` will be thrown.
```
