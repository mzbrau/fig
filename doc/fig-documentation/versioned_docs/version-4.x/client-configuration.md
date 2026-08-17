---
sidebar_position: 2
---

# Client Configuration

`Fig.Client` targets .NET Standard 2.0, so it can be used from .NET Framework and modern .NET hosts. This page focuses on ASP.NET Core (`WebApplication.CreateBuilder`) on .NET 8 or later. Fig API and Fig Web currently run on .NET 10.

For a copy-paste walkthrough, see the [Introduction](./intro.md) or the [Add Fig with AI](./guides/0-add-with-ai.md) playbook. The [AspNetApi example](https://github.com/mzbrau/fig/blob/main/examples/Fig.Examples.AspNetApi/Program.cs) is the canonical reference.

## Bootstrap

1. Add the **[Fig.Client](https://www.nuget.org/packages/Fig.Client)** package.

2. Create a settings class that extends `SettingsBase`. Override `ClientDescription`. Set `ClientName` on `FigOptions`, not on the settings class.

3. Register Fig **after** JSON (and other baseline) providers so Fig wins:

```csharp
using Fig.Client.ExtensionMethods;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "MyApplication";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        options.ClientSecretProviders = [new DockerSecretProvider(), new DpapiSecretProvider()];
        // options.ClientSecretOverride = "a GUID"; // development only
    });

builder.Services.Configure<Settings>(builder.Configuration);
builder.Host.UseFig<Settings>();
```

`UseFig<T>()` registers Fig host workers: configuration health checks, restart, custom actions, lookup table registration, and custom status properties. Do not call obsolete `UseFigValidation` / `UseFigRestart` APIs.

:::tip Last provider wins

Fig should be the last competing configuration provider. Providers registered after Fig can overwrite values in the process without those changes appearing in Fig Web. For a local override that Fig can see, use [client settings override](./features/23-client-settings-override.md).

:::

4. Set `FIG_API_URI` to the Fig API address (comma-separated addresses are tried in order on startup):

```
FIG_API_URI=http://localhost:7281
```

Unset `FIG_API_URI`, or pass `--disable-fig=true`, to run without Fig. That is different from [offline settings](./features/20-offline-settings.md) and from [`--figoffline`](./guides/11-running-offline-without-fig-server.md).

5. Provide a **client secret**. Prefer a GUID, and use the same secret for every instance of the same client. In production, use [client secret providers](./features/28-client-secrets/1-client-secret-providers.md) (Docker, DPAPI, Azure, AWS, or Google). `ClientSecretOverride` and `--secret=` are for development only.

## Startup performance

By default, the client stores a checksum of your settings definition on disk and skips re-registration when the definition has not changed. See [Registration Checksum](./features/37-registration-checksum.md).

## Fig Options

| Option | Description | Default / example |
| ------ | ----------- | ----------------- |
| `ClientName` | Name shown in Fig Web. Required. | `"MyApplication"` |
| `LiveReload` | Update in-memory settings when values change in Fig Web. | `true` |
| `ClientSecretOverride` | In-code secret. Not for production. Prefer a GUID. | a GUID |
| `ClientSecretProviders` | Ordered [secret providers](./features/28-client-secrets/1-client-secret-providers.md). | `[new DockerSecretProvider(), new DpapiSecretProvider()]` |
| `VersionOverride` | Override the version Fig reports. By default Fig reads assembly / file / product version. | `"1.2"` |
| `VersionType` | `Assembly`, `File`, or `Product`. | `Assembly` |
| `AllowOfflineSettings` | Encrypted last-known-good cache when the API is unreachable. | `true` |
| `LoggerFactory` | Enables logging inside Fig.Client. | |
| `CommandLineArgs` | Pass `args` from `Main` so CLI flags work. | `args` |
| `HttpClient` | Optional `HttpClient` (mainly for tests). | |
| `InstanceOverride` | Optional instance name (mainly for tests). Prefer `--instance=` or `FIG_[CLIENTNAME]_INSTANCE`. | |
| `CustomActionPollInterval` | How often the client polls for custom action requests. | `TimeSpan.FromSeconds(5)` |
| `AutomaticallyGenerateHeadings` | Generate headings from categories. | `true` |
| `ApiRequestTimeout` | HTTP timeout to Fig API. Also overridable with `FIG_API_REQUEST_TIMEOUT_SECONDS`. | context-dependent |
| `ApiRetryCount` | Retries before falling back to offline settings. | context-dependent |
| `LookupTableRegistrationDelay` | Delay before registering `ILookupProvider` / `IKeyedLookupProvider` tables. | `TimeSpan.FromSeconds(30)` |

## Command line arguments

Pass `options.CommandLineArgs = args` so Fig can see these flags.

| Argument | Description |
| -------- | ----------- |
| `--disable-fig=true` | Disables Fig entirely. The app starts without contacting the API. |
| `--figoffline` | Run without a Fig server using generated `appsettings` and encrypted secrets. See [Running offline without a Fig server](./guides/11-running-offline-without-fig-server.md). |
| `--printappsettings` | Generate `appsettings.fig.json` (optional `key=value` overrides) and exit. See [AppSettings.json Generation](./features/36-appsettings-generation.md). |
| `--printappconfig` | Log a legacy `app.config` fragment. See [App.config File Generation](./features/25-app-config-generation.md). |
| `--instance=Name` | Select a [named instance](./features/19-instances.md). Takes precedence over `FIG_[CLIENTNAME]_INSTANCE`. |
| `--secret=<value>` | Override the client secret. Not for production. |
| `--setting-definitions` | Export setting definitions to JSON and exit. See [Client Registration History](./features/32-client-registration-history.md). |

```csharp
builder.Configuration.AddFig<Settings>(options =>
{
    options.ClientName = "MyApplication";
    options.CommandLineArgs = args;
});
```

## Three ways to run without a live API

These are easy to confuse:

| Mode | When to use |
| ---- | ----------- |
| [Offline settings](./features/20-offline-settings.md) (`AllowOfflineSettings`) | Fig is still enabled. If the API is briefly down, the client starts from an encrypted cache of the last settings. |
| [`--figoffline`](./guides/11-running-offline-without-fig-server.md) | No Fig server at all. Load previously generated `appsettings` with DPAPI-encrypted secrets. |
| `--disable-fig=true` | Turn Fig off and use ordinary configuration providers. |
