---
sidebar_position: 1
---

# Introduction

Fig is a complete solution for managing settings across .NET microservices. Applications register a strongly typed settings class through the `Fig.Client` NuGet package. The Fig API stores those settings, and the Fig web application is where operators view, edit, and audit them.

:::tip Add with AI

Want an AI coding assistant to wire Fig into your app? Copy the prompt from [Add Fig with AI](./guides/0-add-with-ai.md) and paste it into Cursor, Copilot, Claude Code, or similar.

:::

A Fig 2.0-era product walkthrough is on the [Videos](./overview/videos.md) page. The integration steps on this page reflect the current client API.

## Quick Start

Pick the path that matches how you want to run Fig:

1. **Fastest** — clone the [fig-quick-start](https://github.com/mzbrau/fig-quick-start) repository. It is an Aspire host with Fig API, Fig Web, and a sample application.
2. **Already using Aspire** — add Fig to your AppHost with the [Aspire integration](./guides/9-aspire-integration.md).
3. **Docker Compose** — the compose file in the Fig repository deploys Fig API, Fig Web, and Fig MCP against SQL Server. It is a full deployment, not a one-command local demo.

### Docker Compose

1. Copy [`.env.example`](https://github.com/mzbrau/fig/blob/main/.env.example) to `.env` in the repository root and set `fqdn`, `SA_PWD`, `FIG_DB_PWD`, and the other required values. Optionally set `FIG_MCP_PASSWORD` if you want the MCP container to start authenticated.
2. From the repository root, run `docker compose up`.
3. Open Fig Web at `http://localhost:7148` and log in with user `admin` / password `admin`. Change that password before exposing Fig Web beyond a local machine.

The API listens on `http://localhost:7281`. Compose maps API `7281:8080` and Web `7148:80`.

## Integrate a client

The same steps apply to a new or existing ASP.NET Core project.

1. Create a project if you need one:

```bash
dotnet new webapi
```

2. Add **[Fig.Client](https://www.nuget.org/packages/Fig.Client)**. Add a [secret provider](./features/28-client-secrets/1-client-secret-providers.md) package for production.

3. Create a settings class that extends `SettingsBase`. Put the Fig client name on `FigOptions`, not on this class:

```csharp
using Fig.Client;
using Fig.Client.Abstractions.Attributes;

public class Settings : SettingsBase
{
    public override string ClientDescription => "Example service settings";

    [Setting("My favourite animal")]
    public string FavouriteAnimal { get; set; } = "Cow";

    [Setting("My favourite number")]
    public int FavouriteNumber { get; set; } = 66;

    [Setting("True or false, your choice...")]
    public bool TrueOrFalse { get; set; } = true;
}
```

4. Register Fig as a configuration provider in `Program.cs`. Fig should be the **last** competing provider so it wins over `appsettings.json`.

```csharp
using Fig.Client.ExtensionMethods;

var builder = WebApplication.CreateBuilder(args);

var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "ExampleService";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        // Production: options.ClientSecretProviders = [new DockerSecretProvider(), new DpapiSecretProvider()];
        options.ClientSecretOverride = "be633c90-4744-48c3-82c4-7045b2e172d5"; // development only
    });

builder.Services.Configure<Settings>(builder.Configuration);
builder.Host.UseFig<Settings>();
```

`UseFig<T>()` registers Fig host workers (health, restart, custom actions, lookups). Do not call separate `UseFigValidation` / `UseFigRestart` APIs — they are obsolete.

5. Inject `IOptionsMonitor<Settings>` where the application should receive live updates.

6. Set `FIG_API_URI` to the Fig API address:

```bash
FIG_API_URI=http://localhost:7281
```

Comma-separated addresses are supported; the first reachable address is used for the process lifetime. Unset `FIG_API_URI` (or pass `--disable-fig=true`) to run without Fig.

7. Provide a client secret. Prefer a [secret provider](./features/28-client-secrets/1-client-secret-providers.md) in production. `ClientSecretOverride` is for local development only.

See the [examples](./examples.md) in the source repository and [Client Configuration](./client-configuration.md) for options, CLI flags, and offline modes.

## Packages

| Package | Use |
| ------- | --- |
| [Fig.Client](https://www.nuget.org/packages/Fig.Client) | Add this to applications that Fig should manage |
| [Fig.Client.Abstractions](https://www.nuget.org/packages/Fig.Client.Abstractions) | Attributes and contracts for libraries that must not take a full client dependency |
| [Fig.Client.Testing](https://www.nuget.org/packages/Fig.Client.Testing) | Integration tests and display-script tests |
| [Fig.Aspire](https://www.nuget.org/packages/Fig.Aspire) | Aspire AppHost helpers for Fig API and Fig Web |
| Secret providers | [Docker](./features/28-client-secrets/2-docker-secret-provider.md), [DPAPI](./features/28-client-secrets/3-dpapi-secret-provider.md), [Azure](./features/28-client-secrets/4-azure-secret-provider.md), [AWS](./features/28-client-secrets/5-aws-secret-provider.md), [Google](./features/28-client-secrets/6-google-secret-provider.md) |

Fig MCP is a standalone container/app, not a client NuGet package. See the [Fig MCP Server](./integrations/fig-mcp-server.md) and the [full package list](./nuget-packages.md).
