---
sidebar_position: 10
---

# Settings Binding Verification

One of the most common mistakes when integrating Fig into an application is registering Fig as a configuration provider, but forgetting to bind the settings into the .NET options system. This means that any code consuming `IOptions<T>` or `IOptionsMonitor<T>` will always see default values — even when those values have been updated in Fig.

Fig's testing package includes `FigSettingsBindingVerifier`, a helper that catches this class of misconfiguration with a single assertion in your test suite.

:::note Related Testing
This guide covers verifying that your application correctly binds Fig settings to the .NET options system. For a broader introduction to integration testing with Fig, see the [Integration Testing](./4-integration-testing.md) guide.
:::

## The Problem

A typical Fig setup in `Program.cs` looks like this:

```csharp
// Register Fig as a configuration source
builder.Configuration.AddFig<Settings>(options =>
{
    options.ClientName = "MyApp";
});

// ✅ DON'T FORGET THIS LINE
builder.Services.Configure<Settings>(builder.Configuration);
```

The `Configure<Settings>` call is what connects the Fig-populated `IConfiguration` into the .NET options pipeline. Without it:

- `IOptions<Settings>` always returns the default property values from the class itself.
- `IOptionsMonitor<Settings>` never reflects changes made in Fig.
- The application appears to work because reasonable defaults are often set.

This bug can go undetected for a long time, especially in applications where developers set sensible defaults. `FigSettingsBindingVerifier` exists to catch it automatically.

## Prerequisites

Add the Fig Client Testing package to your test project:

```xml
<PackageReference Include="Fig.Client.Testing" Version="latest" />
```

## Quick Start: Auto-Mutation

The simplest way to verify binding is to use the auto-mutation overload. It requires no knowledge of which properties to test — Fig discovers and mutates all `[Setting]`-decorated properties automatically, reloads the configuration, and asserts that every change is reflected in `IOptionsMonitor<T>`.

```csharp
[Test]
public async Task SettingsShouldBeBoundToOptionsMonitor()
{
    var settings = new Settings();
    var reloader = new ConfigReloader<Settings>();

    // Build the application (same pattern as integration testing)
    var application = new WebApplicationFactory<MyController>().WithWebHostBuilder(builder =>
    {
        builder.DisableFig();
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddIntegrationTestConfiguration(reloader, settings);
        });
    });

    await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
        application.Services,
        reloader,
        settings);
}
```

If `services.Configure<Settings>(builder.Configuration)` is missing from `Program.cs`, this test will fail with a descriptive message listing every property that did not reload.

:::tip Automatically Adapts to Change
Because auto-mutation discovers properties by reflection, the test continues to cover newly added settings without any manual updates. Add a new `[Setting]` property to your class and the verifier will start testing it immediately.
:::

## Supported Types

Auto-mutation handles the following property types (including their `Nullable<T>` equivalents):

| Type | Mutation Applied |
|------|-----------------|
| `string` | Appends `"_fig_mutated"` |
| `bool` | Flips the value |
| `int`, `long`, `short`, `byte`, etc. | Adds 1 |
| `double`, `float`, `decimal` | Adds 1.0 |
| `Guid` | Generates a new `Guid.NewGuid()` |
| `DateTime`, `DateTimeOffset` | Adds 1 day |
| `TimeSpan` | Adds 1 second |
| Enum | Cycles to the next declared value |

Collection properties (`List<T>`, `IEnumerable<T>`) and complex-object properties are skipped. Use [explicit mutations](#explicit-mutations) for those.

## Explicit Mutations

When you need fine-grained control — for example, to test a specific property, a collection setting, or a named options registration — use the explicit overload:

```csharp
[Test]
public async Task LocationShouldReloadViaOptionsMonitor()
{
    await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
        application.Services,
        reloader,
        settings,
        s => s.Location = $"Test-{Guid.NewGuid():N}",  // mutation
        s => s.Location);                               // value to verify
}
```

This overload:
1. Applies your mutation to `settings`
2. Reloads the configuration via the `ConfigReloader`
3. Polls `IOptionsMonitor<Settings>` until the selected property reflects the new value, or times out

### Explicit Mutation with a Custom Timeout

```csharp
await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
    services,
    reloader,
    settings,
    s => s.ApiTimeoutSeconds = 99,
    s => s.ApiTimeoutSeconds,
    timeout: TimeSpan.FromSeconds(5));
```

## Verifying Initial Binding (IOptions&lt;T&gt;)

If your application injects `IOptions<T>` (not `IOptionsMonitor<T>`), use `VerifyOptionsBound` to check that initial values are correctly populated:

```csharp
[Test]
public void SettingsShouldBeReflectedInOptions()
{
    var settings = new Settings { Location = "London" };

    // Build services with the reloadable configuration provider
    var configuration = new ConfigurationBuilder()
        .AddIntegrationTestConfiguration(new ConfigReloader<Settings>(), settings)
        .Build();

    var services = new ServiceCollection();
    services.Configure<Settings>(configuration);
    var provider = services.BuildServiceProvider();

    FigSettingsBindingVerifier.VerifyOptionsBound(provider, settings, s => s.Location);
}
```

:::warning IOptions&lt;T&gt; vs IOptionsMonitor&lt;T&gt;
`IOptions<T>` is resolved once at startup and never updates. If your application needs to react to live changes from Fig, use `IOptionsMonitor<T>` and verify with `VerifyOptionsMonitorReloadsAsync`.
:::

## Section-Bound Options

Fig supports `[ConfigurationSectionOverride]` to map individual properties into non-default configuration sections. Applications sometimes bind these sections into separate options classes:

```csharp
// Program.cs
builder.Services.Configure<ConnectionStringOptions>(
    builder.Configuration.GetSection("ConnectionStrings"));
```

Verify this with the three-type overload:

```csharp
[Test]
public async Task ConnectionStringShouldReloadViaItsSection()
{
    await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync<Settings, ConnectionStringOptions, string>(
        application.Services,
        reloader,
        settings,
        s => s.Database.ConnectionString = "Server=test",   // mutate the Fig setting
        s => s.Database.ConnectionString,                    // expected value selector
        o => o.DefaultConnection!);                          // actual value from section options
}
```

## Named Options

If your application uses named options registrations:

```csharp
// Program.cs
builder.Services.Configure<Settings>("primary", builder.Configuration);
```

Pass the name to either overload:

```csharp
// Auto-mutation with named options
await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
    services, reloader, settings, optionsName: "primary");

// Explicit mutation with named options
await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
    services, reloader, settings,
    s => s.Location = "Tokyo",
    s => s.Location,
    optionsName: "primary");
```

## Full Example: ASP.NET Core Integration Test

Here is a complete example based on the Fig.Examples.AspNetApi project:

```csharp
// IntegrationTestBase.cs
public abstract class IntegrationTestBase
{
    protected readonly Settings Settings = new();
    protected readonly ConfigReloader<Settings> ConfigReloader = new();
    protected HttpClient? Client;
    protected WebApplicationFactory<WeatherForecastController>? Application;

    [OneTimeSetUp]
    public void FixtureSetup()
    {
        Application = new WebApplicationFactory<WeatherForecastController>().WithWebHostBuilder(builder =>
        {
            builder.DisableFig();
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddIntegrationTestConfiguration(ConfigReloader, Settings);
                config.Build();
            });
        });

        Client = Application.CreateClient();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        Client?.Dispose();
        Application?.Dispose();
    }
}

// IntegrationTests.cs
public class IntegrationTests : IntegrationTestBase
{
    // Verify ALL settings are correctly bound — adapts automatically as settings are added
    [Test]
    public async Task AllSettingsShouldBeBoundToOptionsMonitor()
    {
        await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
            Application!.Services,
            ConfigReloader,
            Settings);
    }

    // Verify a specific property end-to-end through the HTTP layer
    [Test]
    public async Task LocationShouldBeReflectedInApiResponse()
    {
        await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
            Application!.Services,
            ConfigReloader,
            Settings,
            s => s.Location = $"Test-{Guid.NewGuid():N}",
            s => s.Location);
    }
}
```

## Understanding Failures

When verification fails, `FigSettingsBindingVerificationException` is thrown with a message that identifies the problem and hints at the fix:

```
Fig settings binding verification failed for MyApp.Settings (default options).
2 properties did not reload correctly:
  - 'Location': expected "London_fig_mutated", but found "London"
  - 'ApiTimeoutSeconds': expected 31, but found 30
Ensure the application registers options binding, for example
services.Configure<TOptions>(configuration) or
services.Configure<TOptions>(configuration.GetSection(...)).
```

The most common cause is a missing `services.Configure<Settings>(configuration)` call in `Program.cs`. The exception message always includes this hint.

## Why Add This Test?

Adding a binding verification test provides a permanent regression guard against a class of misconfiguration that is easy to introduce and hard to notice:

- **Refactoring risk** — someone removes or reorganises DI registrations and the `Configure<T>` call gets lost.
- **New service registration** — a new class or module injects `IOptions<T>` without the corresponding binding being set up.
- **Onboarding** — developers new to the codebase may not know the `Configure<T>` call is required.

With a single test that adapts automatically as settings evolve, you get continuous coverage of this critical wiring at essentially zero maintenance cost.
