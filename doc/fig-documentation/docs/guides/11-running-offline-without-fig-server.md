---
sidebar_position: 11
---

# Running an Application Offline Without a Fig Server

This guide walks through how to run a Fig-configured application without a Fig server while still protecting sensitive passwords — avoiding plain text secrets in `appsettings.json`.

## Overview

The offline workflow has two phases:

1. **Generate** — Run the application once with `--printappsettings` to produce an `appsettings.json` file with encrypted secrets
2. **Run Offline** — Start the application with `--figoffline`; Fig reads configuration from `appsettings.json` and automatically decrypts any `_FigEncrypted` values

## Prerequisites

- Windows machine (DPAPI encryption is Windows-only)
- The application must use Fig.Client and pass `CommandLineArgs` in `FigOptions`
- The same Windows user account must be used both to generate the file and to run the application

## Step 1: Configure Your Application

Ensure your `Program.cs` (or equivalent startup code) passes the command line arguments to Fig:

```csharp
var configuration = new ConfigurationBuilder()
    .AddFig<MySettings>(o =>
    {
        o.ClientName = "My App";
        o.CommandLineArgs = args;  // Required for offline features
        // ... other options
    })
    .Build();
```

And also update your `IHostBuilder` to support offline mode:

```csharp
var host = Host.CreateDefaultBuilder(args)
    .UseFig<MySettings>()
    // ...
    .Build();
```

## Step 2: Generate the appsettings.json

Run your application with `--printappsettings`, providing values for any settings you want to override (including secrets):

```bash
myapp.exe --printappsettings ApiUrl=https://api.company.com Password=MySecretPass123
```

:::tip
You must run this command **as the same Windows user account** that will run the application in production. DPAPI encryption is tied to the user identity.
:::

The command will:
1. Create `appsettings.json` in the **current working directory**
2. Print the file path to the console
3. Exit immediately (the application does not start)

### Example Output

```json
{
  "ApiUrl": "https://api.company.com",
  "MaxRetries": "3",
  "Password_FigEncrypted": "AQAAANCMnd8BFdERjH..."
}
```

Notice that `Password` appears as `Password_FigEncrypted` — the value is DPAPI-encrypted and safe to store in configuration files or source control (though this is not recommended).

Settings marked `[Secret]` are always stored encrypted. Other settings use their default values unless overridden.

## Step 3: Run the Application Offline

Place the generated `appsettings.json` next to your application and start it with `--figoffline`:

```bash
myapp.exe --figoffline
```

When `--figoffline` is active, Fig:

- **Does not** attempt to connect to the Fig API
- **Does not** register or update settings
- **Does not** start any background workers (health reporting, live reload, etc.)
- **Does** scan the configuration for keys ending in `_FigEncrypted`
- **Does** decrypt those values using DPAPI and make them available under their original names

Your application reads all other configuration (non-secret settings, environment variables, etc.) normally from `appsettings.json` and other standard configuration providers.

## Comparison: `--figoffline` vs `--disable-fig`

| Feature | `--figoffline` | `--disable-fig=true` |
|---------|---------------|----------------------|
| Connects to Fig API | ❌ | ❌ |
| Registers settings | ❌ | ❌ |
| Decrypts `_FigEncrypted` values | ✅ | ❌ |
| Uses standard config providers | ✅ | ✅ |
| Background workers | ❌ | ❌ |

Use `--figoffline` when you have encrypted settings in `appsettings.json`. Use `--disable-fig=true` when you want to use standard configuration entirely without any Fig involvement.

## Security Considerations

- DPAPI-encrypted values are tied to the **user account** on the **machine** where they were created
- The encrypted value **cannot** be used on a different machine or by a different user
- If you need to rotate the encrypted values (e.g., password changed), regenerate the `appsettings.json` using `--printappsettings` with the new value
- Encrypted `appsettings.json` files are safer than plain text but should still be protected with appropriate file system permissions

## Troubleshooting

**Secret setting shows the default value instead of the configured one**  
Ensure that:
- The application is running as the same Windows user who generated the file
- The `_FigEncrypted` key in `appsettings.json` is properly formed (no typos in the suffix)
- The `appsettings.json` is in a location that the application reads from

**Settings are not being read from appsettings.json**  
The application's startup must add `appsettings.json` as a configuration source before calling `AddFig<T>()`. Example:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)  // Add BEFORE AddFig
    .AddFig<MySettings>(o =>
    {
        o.ClientName = "My App";
        o.CommandLineArgs = args;
    })
    .Build();
```

**`--printappsettings` generates no secret entries on Linux/Mac**  
DPAPI is only available on Windows. Run the generation step on a Windows machine, then transfer the `appsettings.json` to your target environment.
