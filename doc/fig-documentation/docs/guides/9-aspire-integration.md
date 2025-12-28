---
sidebar_position: 9
---

# .NET Aspire Integration

.NET Aspire is a cloud-ready stack for building observable, production-ready, distributed applications. Fig integrates seamlessly with .NET Aspire through the `Fig.Aspire` NuGet package, allowing you to easily add Fig's centralized configuration management to your Aspire applications.

## Prerequisites

Before you can use Fig with .NET Aspire, you need to:

1. Have a .NET Aspire application set up (AppHost project)
2. Install the Fig.Aspire NuGet package in your AppHost project:

```xml
<PackageReference Include="Fig.Aspire" Version="latest" />
```

## Quick Start

The simplest way to add Fig to your Aspire application is to use the convenience method that adds both the Fig API and Fig Web:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var figApi = builder.AddFigApi("fig-api");
var figWeb = builder.AddFigWeb("fig-web")
    .WithFigApiReference(figApi);

// Reference Fig API in your services
builder.AddProject<Projects.InventoryService>("inventoryservice")
    .WithReference(figApi);

builder.AddProject<Projects.OrderService>("orderservice")
    .WithReference(figApi);

builder.Build().Run();
```

When you use `WithReference(figApi)`, an environment variable named `FIG_API_URI` is automatically injected into your service, pointing to the Fig API endpoint.

### Configuration Options

The `AddFigApi` method supports several configuration options:

```csharp
var figApi = builder.AddFigApi(
    name: "fig-api",           // Resource name
    port: 7281,                // Optional: Host port (random if not specified)
    tag: "latest"              // Optional: Docker image tag
);
```

The `AddFigWeb` method supports similar configuration options:

```csharp
var figWeb = builder.AddFigWeb(
    name: "fig-web",           // Resource name
    port: 7148,                // Optional: Host port (random if not specified)
    tag: "latest"              // Optional: Docker image tag
);
```

## Database Configuration

By default, Fig API uses an embedded SQLite database. For production use, you can configure it to use SQL Server.

1. Add your connection string to the AppHost's configuration (e.g., user secrets):

```json
{
  "ConnectionStrings": {
    "FigDb": "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;"
  }
}
```

2. In your AppHost `Program.cs`, register the connection string and pass it to the Fig API resource:

```csharp
var figDb = builder.AddConnectionString("FigDb");

var figApi = builder.AddFigApi("fig-api")
    .WithReference(figDb, "Fig");
```

> Note: We use `WithReference(figDb, "Fig")` to inject the connection string with the name "Fig", which is what Fig API expects.

## Alternative

If you don't want to use the Fig provided extension methods, Fig can also be added as docker containers (this is what the extensions are doing behind the scenes anyway)

```csharp
var figApi = builder.AddContainer("fig-api", "mzbrau/fig-api")
    .WithImageTag("latest")
    .WithHttpEndpoint(port: 7281, targetPort: 8080, name: "http")
    .WithLifetime(ContainerLifetime.Persistent);

var figWeb = builder.AddContainer("fig-web", "mzbrau/fig-web")
    .WithImageTag("latest")
    .WithHttpEndpoint(port: 7148, targetPort: 80, name: "http")
    .WithEnvironment("FIG_API_URI", figApi.GetEndpoint("http"))
    .WaitFor(figApi)
    .WithLifetime(ContainerLifetime.Persistent);
```

## Client Configuration in Your Services

Once your services reference the Fig API, you need to configure the Fig client in your service's `Program.cs`:

```csharp
using Fig.Client;
using Fig.Client.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add Fig as a configuration provider
builder.Configuration.AddFig<AppSettings>(options =>
{
    options.ClientName = "MyService";
    options.ClientSecret = "your-client-secret";
    // FIG_API_URI is automatically set by Aspire
});

var app = builder.Build();
app.Run();
```

The `FIG_API_URI` environment variable is automatically injected by Aspire, so you don't need to manually configure it.

## Managing Aspire Set Settings

If Aspire is automatically setting some environment related settings, Fig may need to be configured to consume that setting.

For example, imagine your application is using a NATS message bus and Aspire is wiring it up for you.

```csharp
var nats = builder.AddNats("nats")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.InventoryService>("inventoryservice")
    .WithReference(nats);
```

In that case, an environment variable will be set with a specific address and Fig will need to be configured to consume that environment variable. You can use the [ConfigurationSectionOverride](../features/settings-management/4-configuration-section.md) feature to set the setting in the correct location.

For example:

```csharp
[Setting("NATS server URL (e.g. nats://localhost:4222)")]
[ConfigurationSectionOverride("ConnectionStrings", "nats")]
public string NatsUrl { get; set; } = "nats://localhost:4222";
```

This will set the value from Aspire and mark the setting as externally managed.

## Troubleshooting

### Service Can't Connect to Fig API

If your service can't connect to Fig API:

1. Verify the `FIG_API_URI` environment variable is set in your service by checking the Aspire dashboard
2. Ensure your service has a reference to the Fig API: `.WithReference(figApi)`
3. Check the Fig API container logs in the Aspire dashboard for any startup errors

### Fig Web Can't Connect to Fig API

If Fig Web shows connection errors:

1. Ensure you used `.WithFigApiReference(figApi)` when configuring Fig Web
2. Check that Fig API is healthy and running in the Aspire dashboard
3. Verify the Fig API container is accessible on the configured port

### Containers Keep Restarting

If Fig containers keep restarting:

1. Check container logs in the Aspire dashboard for error messages
2. Ensure you have enough resources (memory, CPU) allocated for Docker
3. Verify the Docker images are pulling correctly (check your internet connection)
