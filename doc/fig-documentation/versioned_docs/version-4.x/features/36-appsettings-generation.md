---
sidebar_position: 36
sidebar_label: AppSettings.json Generation
---

# AppSettings.json Generation

Fig can generate an `appsettings.json` file from your application's settings class, including encrypted secrets. This is useful for running applications in an offline mode without a Fig server.

## Usage

Run your application with the `--printappsettings` command line argument:

```bash
myapp --printappsettings
```

This generates an `appsettings.fig.json` file in the current working directory containing all settings with their default values.

### Providing Value Overrides

You can override default values by providing `key=value` pairs after the flag:

```bash
myapp --printappsettings MySetting=myvalue AnotherSetting="different value"
```

Overrides are matched case-insensitively to setting names.

### Example

Given a settings class:

```csharp
public class MySettings : SettingsBase
{
    [Setting("API URL")]
    public string ApiUrl { get; set; } = "https://example.com";

    [Setting("Max Retries")]
    public int MaxRetries { get; set; } = 3;

    [Setting("Password")]
    [Secret]
    public string Password { get; set; } = "";
}
```

Running:

```bash
myapp --printappsettings ApiUrl=https://api.mycompany.com MaxRetries=5 Password=MySecret
```

Produces an `appsettings.fig.json` file containing:

```json
{
  "ApiUrl": "https://api.mycompany.com",
  "MaxRetries": "5",
  "Password_FigEncrypted": "<DPAPI encrypted value of MySecret>"
}
```

## Secret Settings

Settings marked with the `[Secret]` attribute receive special handling:

- The plain setting name is **not** included in the file
- Instead, the setting appears as `<SettingName>_FigEncrypted` with a [DPAPI](https://learn.microsoft.com/en-us/dotnet/standard/security/how-to-use-data-protection)-encrypted value
- The encrypted value is tied to the **currently logged-in user** on the machine where the command is run

:::warning Windows Only
DPAPI encryption is supported only on Windows. On Linux and macOS, secret settings are omitted from the generated file and a warning is printed to the console.
:::

## Using the Generated File Offline

Once you have an `appsettings.fig.json`, you can rename if to `appsettings.json` and run your application without a Fig server using the `--figoffline` argument. See the [Running Offline Without a Fig Server](../guides/11-running-offline-without-fig-server.md) guide for the complete workflow.

## Passing Arguments to Fig

To use these command line arguments, pass them through `FigOptions.CommandLineArgs` in your application setup. If your application already uses a `DpapiSecretProvider` (from the `Fig.Client.SecretProvider.Dpapi` package), DPAPI encryption is enabled automatically — no extra configuration is required:

```csharp
var configuration = new ConfigurationBuilder()
    .AddFig<MySettings>(o =>
    {
        o.ClientName = "My App";
        o.CommandLineArgs = args;  // Pass args from Program.cs
        o.ClientSecretProviders = [new DpapiSecretProvider()];  // Enables DPAPI for both client secrets and appsettings encryption
        // ... other options
    })
    .Build();
```

The `DpapiSecretProvider` class is in the `Fig.Client.SecretProvider.Dpapi` NuGet package and implements both `IClientSecretProvider` and `IAppSettingsEncryptionProvider`. If no provider that implements `IAppSettingsEncryptionProvider` is found in `ClientSecretProviders`, secret settings are omitted from the generated file and a warning is printed to the console.

:::info Extensible encryption
The `IAppSettingsEncryptionProvider` interface (in `Fig.Client.Contracts`) allows alternative encryption providers. Any `IClientSecretProvider` that also implements `IAppSettingsEncryptionProvider` will be detected automatically — simply add it to `FigOptions.ClientSecretProviders`.
:::
