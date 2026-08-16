---
sidebar_position: 0
slug: /guides/add-with-ai
---

# Add Fig with AI

You can ask your AI coding assistant to integrate Fig.Client into your application. Share this page URL and the prompt below — the assistant will inspect how configuration is loaded today, create or migrate a settings class, wire Fig into startup, and ask which Fig features you want before finishing.

## Copy this prompt

Paste this into Cursor, Copilot, Claude Code, or any coding agent that can edit your repository:

```text
Set up Fig.Client in this application. Follow the setup playbook at
https://www.figsettings.com/docs/guides/add-with-ai
Inspect how configuration is loaded today, create or migrate a Settings
class, wire AddFig/UseFig, then ask me which Fig features to enable
(secrets, validation, categories, lookups, etc.) before finishing.
```

---

## Setup playbook (for AI assistants)

When helping a user add Fig to a .NET application, follow these phases in order. Prefer the patterns in this playbook and the AspNetApi example over older Quick Start snippets that may put `ClientName` on the settings class or call separate `UseFigValidation` / `UseFigRestart` APIs.

Canonical reference implementation:

- [Fig.Examples.AspNetApi Program.cs](https://github.com/mzbrau/fig/blob/main/examples/Fig.Examples.AspNetApi/Program.cs)
- [Fig.Examples.AspNetApi Settings.cs](https://github.com/mzbrau/fig/blob/main/examples/Fig.Examples.AspNetApi/Settings.cs)

### Phase 0 — Discover

1. Detect the host type:
   - ASP.NET Core minimal hosting (`WebApplication.CreateBuilder`)
   - Generic host / worker service
   - Console app with `ConfigurationBuilder`
   - .NET Aspire AppHost + service projects
   - .NET Framework (`FigConfigurationManager<T>` — rare; use only when the project targets Framework)
2. Locate existing configuration sources:
   - `appsettings.json` / `appsettings.*.json`
   - Options classes bound via `Configure<T>` / `IOptions<T>`
   - Environment variables and command-line args
   - Nested sections used by libraries (Serilog, ConnectionStrings, YARP, etc.)
3. Note target framework, whether `Fig.Client` is already referenced, and whether `FIG_API_URI` is already set.
4. Prefer **one Fig settings class per Fig client name**.

If multiple runnable projects or settings roots exist, **stop and ask** which project and which configuration to migrate.

### Phase 1 — Fig host

Fig.Client needs a running Fig API (and usually Fig Web for administration).

**Stop and ask** the user:

1. Is Fig API/Web already running?
2. What is the Fig API URI? (common local docker default: `https://localhost:7281`)

Guidance:

- If Fig is already running, use their URI and continue.
- If not, point them at one of these — do **not** invent a full custom Fig deployment unless they ask:
  - [fig-quick-start](https://github.com/mzbrau/fig-quick-start) (Aspire sample with Fig + a demo app)
  - Docker Compose in the [fig repository](https://github.com/mzbrau/fig)
  - [.NET Aspire Integration](./9-aspire-integration.md) when the app already uses Aspire
- Local Fig Web (Docker defaults): `http://localhost:7148` (user `admin` / password `admin`) for isolated local development only. Change the password before exposing Fig Web.

### Phase 2 — Settings class

Create a settings class that extends `SettingsBase`.

Rules:

- Override `ClientDescription` (markdown or plain text describing the client).
- Put **`ClientName` on `FigOptions` during bootstrap**, not on the settings class.
- Mark every Fig-managed property with `[Setting("...description...")]`.
- Property initializers are the defaults Fig registers.
- Keep host-only config local when it is not useful to manage in Fig (Kestrel endpoints, some Serilog sink wiring). Prefer Fig for application settings operators will change.
- Preserve nested library sections with `[ConfigurationSectionOverride]` when libraries bind nested keys (see Phase 4 / Phase 6).

**Stop and ask** which settings keys should be managed in Fig versus left in local `appsettings` / environment config.

Minimal example:

```csharp
using System;
using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Abstractions.Validation;

public class Settings : SettingsBase
{
    public override string ClientDescription => "My application settings";

    [Setting("Primary database connection string")]
    [Secret]
    [Category("Database", CategoryColor.Blue)]
    public string PrimaryDbConnectionString { get; set; } =
        "Server=localhost;Database=MyApp;";

    [Setting("API timeout in seconds")]
    [Category("Api", CategoryColor.Red)]
    [Validation(ValidationType.GreaterThanZero)]
    public int ApiTimeoutSeconds { get; set; } = 30;

    [Setting("Feature enabled")]
    [Category("Features", CategoryColor.Green)]
    public bool FeatureEnabled { get; set; } = true;

    public override IEnumerable<string> GetValidationErrors()
    {
        return Array.Empty<string>();
    }
}
```
Refactoring guidance:

- Flatten existing Options classes into one Fig settings type when practical.
- Or keep nested objects with `[NestedSetting]` on the parent property and `[Setting]` on nested members.
- Map a flat Fig property into a nested configuration path with `[ConfigurationSectionOverride("Section", "Key")]`.
- For collections, set defaults carefully; use `defaultValueMethodName` on `[Setting]` when needed.

### Phase 3 — Bootstrap

1. Add the NuGet package **[Fig.Client](https://www.nuget.org/packages/Fig.Client)** to the application project.
2. Add secret-provider packages only after Phase 5 choices (Docker / DPAPI / cloud).
3. Register Fig **after** JSON (and other baseline) providers so Fig wins.
4. Bind options and call `UseFig<T>()`.

Canonical ASP.NET Core pattern:

```csharp
using Fig.Client.ExtensionMethods;

var builder = WebApplication.CreateBuilder(args);

var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

builder.Configuration
    .AddFig<Settings>(options =>
    {
        options.ClientName = "MyApp"; // Fig client name shown in Fig Web
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        // Prefer secret providers in production (Phase 5).
        // options.ClientSecretProviders = [new DockerSecretProvider(), new DpapiSecretProvider()];
        options.ClientSecretOverride = "<generate-a-guid-without-dashes-32+chars>"; // non-production only
    });

builder.Services.Configure<Settings>(builder.Configuration);
builder.Host.UseFig<Settings>();

var app = builder.Build();
app.Run();
```

Notes:

- `UseFig<T>()` registers Fig host workers (health, restart, custom actions, lookups). Do **not** add obsolete separate `UseFigValidation` / `UseFigRestart` calls.
- If the client name is not obvious from the project name, **ask** the user what `ClientName` to use.
- To run **without** the Fig configuration provider (useful for some tests), omit / unset `FIG_API_URI`, or pass `--disable-fig=true`. That fully disables Fig — it is different from `AllowOfflineSettings`, which keeps Fig active and loads previously cached settings when the API is unreachable.

### Phase 4 — Feature questions (maximize value)

**Stop and ask** before applying optional features. Present this checklist in one message, and recommend defaults based on what you discovered:

1. **Secrets** — Mark connection strings, API keys, and passwords with `[Secret]`?
2. **Validation** — Add `[Validation]`, `[ValidateGreaterThan]`, `[ValidateLessThan]`, `[ValidateIsBetween]`, `[ValidateCount]`, or `[ValidateSqlServerConnectionString]` where appropriate?
3. **Categories and headings** — Group settings with `[Category]` / `[Heading]`?
4. **Dropdowns** — Use `[ValidValues]` for enums or fixed string lists?
5. **Conditional UI** — Use `[DependsOn]` / `[EnablesSettings]` when settings only apply in some modes?
6. **Lookups** — Static Fig lookup tables, or app-defined `ILookupProvider` / `IKeyedLookupProvider`?
7. **Display scripts** — JavaScript UI rules for advanced conditional UX? (defer unless requested)
8. **Custom actions** — Ops buttons in Fig Web via `ICustomAction`? (defer unless requested)
9. **Nested / section override** — Keep nested JSON shape for libraries that bind sections?
10. **Offline settings** — Keep `AllowOfflineSettings` default (`true`) so Fig can load cached settings when the API is briefly unavailable? (This is not the same as `--disable-fig=true`, which turns Fig off entirely.)
11. **Testing** — Add [Fig.Client.Testing](./4-integration-testing.md) helpers / [settings binding verification](./10-settings-binding-verification.md)?

If the user is unsure, enable **secrets + validation + categories + valid values** for anything that clearly fits. Defer display scripts, custom actions, and lookups unless the app already has dynamic lists or operational actions.

#### Attribute quick reference

| Goal | Attribute / API |
| --- | --- |
| Manage in Fig | `[Setting("description")]` |
| Mask in UI / treat as secret | `[Secret]` |
| Group in UI | `[Category(...)]`, `[Heading(...)]` |
| Regex or built-in validation | `[Validation(ValidationType.NotEmpty)]`, `[Validation(@"^...$", "message")]` |
| Numeric / count validation | `[ValidateGreaterThan]`, `[ValidateLessThan]`, `[ValidateIsBetween]`, `[ValidateCount]` |
| Dropdown | `[ValidValues("A", "B")]` or `[ValidValues(typeof(MyEnum))]` |
| Show only when another setting matches | `[DependsOn(nameof(Other), "Value")]` |
| Nested object settings | `[NestedSetting]` on parent |
| Map into nested IConfiguration path | `[ConfigurationSectionOverride("Section", "Key")]` |
| Dynamic dropdown from app | `[LookupTable("Name", LookupSource.ProviderDefined)]` + `ILookupProvider` |
| Cross-setting validation | Override `GetValidationErrors()` on `SettingsBase` |
| Live updates in app code | Inject `IOptionsMonitor<Settings>` |

`ValidationType` helpers include: `IpAddress`, `IpAddressAndPort`, `StrongPassword`, `NotEmpty`, `GreaterThanZero`.

Rich end-to-end example: [AspNetApi Settings.cs](https://github.com/mzbrau/fig/blob/main/examples/Fig.Examples.AspNetApi/Settings.cs).

Example with several high-value attributes:

```csharp
using System;
using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Abstractions.Validation;
using Microsoft.Extensions.Logging;

public class Settings : SettingsBase
{
    public override string ClientDescription => "Order service";

    [Setting("Primary database connection string")]
    [Category(Category.Database)]
    [Secret]
    [ValidateSqlServerConnectionString]
    public string PrimaryDbConnectionString { get; set; } =
        "Server=localhost;Database=Orders;";

    [Setting("Minimum log level")]
    [Category(Category.Logging)]
    [ValidValues(typeof(LogLevel))]
    public LogLevel MinLogLevel { get; set; } = LogLevel.Information;

    [Setting("Log file path")]
    [Category(Category.Logging)]
    [Validation(ValidationType.NotEmpty)]
    [DependsOn(nameof(MinLogLevel), "Information", "Debug")]
    public string LogFilePath { get; set; } = "/var/log/orders.log";

    [Setting("External API base URL")]
    [Category(Category.ApiIntegration)]
    [Validation(ValidationType.NotEmpty)]
    public string ExternalApiUrl { get; set; } = "https://api.example.com";

    [Setting("API timeout in seconds")]
    [Category(Category.ApiIntegration)]
    [Heading("API Configuration", CategoryColor.Red)]
    [Validation(ValidationType.GreaterThanZero)]
    public int ApiTimeoutSeconds { get; set; } = 30;

    [Setting("Override for system logs")]
    [ConfigurationSectionOverride("Serilog:Override", "System")]
    [ValidValues(typeof(LogLevel))]
    [Category("General", CategoryColor.Green)]
    public LogLevel SystemLogOverride { get; set; } = LogLevel.Warning;

    public override IEnumerable<string> GetValidationErrors()
    {
        return Array.Empty<string>();
    }
}
```
Apply only the features the user accepted. Do not enable display scripts, custom actions, or lookups unconditionally.

### Phase 5 — Client secret and API URI

Fig requires a client secret (random string, at least 32 characters — a GUID without dashes works).

**Stop and ask**:

1. Use a development `ClientSecretOverride` for now, or wire a secret provider?
2. Confirm `FIG_API_URI` (from Phase 1).

Guidance:

- **Development:** generate a secret and set `options.ClientSecretOverride`. Warn clearly that this is not for production.
- **Production:** use secret providers (Docker `FIG_<CLIENT>_SECRET`, DPAPI, Azure/AWS/Google packages). See [Client Secret Providers](../features/28-client-secrets/1-client-secret-providers.md).
- Set environment variable:

```bash
FIG_API_URI=https://localhost:7281
```

Multiple API URIs can be comma-separated for failover.

### Phase 6 — Consume settings

1. Prefer `IOptionsMonitor<T>` so the app receives live Fig updates:

```csharp
public class OrdersController : ControllerBase
{
    private readonly IOptionsMonitor<Settings> _settings;

    public OrdersController(IOptionsMonitor<Settings> settings)
    {
        _settings = settings;
    }

    [HttpGet("timeout")]
    public int GetTimeout() => _settings.CurrentValue.ApiTimeoutSeconds;
}
```

2. Update call sites that previously read migrated keys from `IConfiguration["Key"]` or old Options types.
3. Keep nested library binding working with `[ConfigurationSectionOverride]` (Serilog, YARP, ConnectionStrings). See [Fig.Examples.Yarp](https://github.com/mzbrau/fig/tree/main/examples/Fig.Examples.Yarp).
4. If the user opted into lookups or custom actions, register implementations in DI before `UseFig`:

```csharp
builder.Services.AddSingleton<ICustomAction, MyAction>();
builder.Services.AddSingleton<ILookupProvider, MyLookupProvider>();
```

### Phase 7 — Validate

Before finishing, verify:

- [ ] Project builds
- [ ] `Fig.Client` package referenced
- [ ] Settings class extends `SettingsBase` with `[Setting]` properties
- [ ] `ClientName` is set on `FigOptions` (not on the settings class)
- [ ] `AddFig` is registered after JSON providers
- [ ] `Configure<Settings>` and `UseFig<Settings>` are present
- [ ] `FIG_API_URI` is documented/set for local run
- [ ] Client secret strategy is in place (override for dev, or providers)
- [ ] Requested attributes applied (secrets, validation, categories, etc.)
- [ ] Consuming code uses `IOptionsMonitor<Settings>` where live reload matters

Summarize changes and explain how to verify:

1. Start Fig API/Web if needed.
2. Run the application with `FIG_API_URI` set.
3. Open Fig Web (local docker default `http://localhost:7148`) and confirm the client appears.
4. Change a non-secret setting and confirm the app picks it up via `IOptionsMonitor` (when live update is enabled on the setting).
5. Confirm secret settings are masked in the UI.
6. Confirm validation rejects invalid values when validation was enabled.

---

## Do not

- Put `ClientName` on the settings class — set `options.ClientName` in `AddFig`
- Call obsolete separate `UseFigValidation` / `UseFigRestart` — use `UseFig<T>`
- Register Fig before JSON/base providers when Fig should win — Fig should be last among competing providers
- Use `ClientSecretOverride` as the production secret strategy
- Migrate every `appsettings` key blindly — leave host/logging plumbing local unless the user wants it in Fig
- Enable display scripts, custom actions, or lookups without asking
- Copy outdated Quick Start snippets that conflict with this playbook
- Confuse this developer onboarding flow with in-product **Fig Assistant** (an admin chatbot inside Fig Web)

---

## Further reading

- [Introduction / Quick Start](../intro.md)
- [Client Configuration](../client-configuration.md)
- [Client Secret Providers](../features/28-client-secrets/1-client-secret-providers.md)
- [Validation](../features/settings-management/20-validation.md)
- [Valid Values](../features/settings-management/19-valid-values.md)
- [Categories](../features/settings-management/2-category.md)
- [Dependent Settings](../features/settings-management/7-dependent-settings.md)
- [Display Scripts](../features/settings-management/8-display-scripts.md)
- [Nested Settings](../features/settings-management/13-nested-settings.md)
- [Lookup Tables](../features/11-lookup-tables.md)
- [Custom Actions](../features/27-custom-actions.md)
- [Offline Settings](./11-running-offline-without-fig-server.md)
- [.NET Aspire Integration](./9-aspire-integration.md)
- [Integration Testing](./4-integration-testing.md)
- [Settings Binding Verification](./10-settings-binding-verification.md)
- [Examples](../examples.md)
