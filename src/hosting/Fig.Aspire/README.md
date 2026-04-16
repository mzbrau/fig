# Fig.Aspire

.NET Aspire hosting integration for [Fig](https://www.figsettings.com) - Centralized Settings Management for .NET Microservices.

## Installation

```bash
dotnet add package Fig.Aspire
```

## Usage

### Adding Fig API

Add the Fig API container to your Aspire AppHost:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var figApi = builder.AddFigApi("fig-api");

builder.AddProject<Projects.MyService>("myservice")
    .WithReference(figApi);

builder.Build().Run();
```

When you use `WithReference(figApi)`, an environment variable named `FIG_API_URI` will be automatically injected into your service with the Fig API's HTTP endpoint URL.

### Adding Fig Web

Add the Fig Web container for the web-based configuration UI:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var figApi = builder.AddFigApi("fig-api");
var figWeb = builder.AddFigWeb("fig-web")
    .WithFigApiReference(figApi);

builder.Build().Run();
```

## Container Images

This integration uses the following container images from Docker Hub by default:

- **Fig API**: `mzbrau/fig-api`
- **Fig Web**: `mzbrau/fig-web`

### Using a Custom Registry

If your organisation mirrors or hosts Fig images in a private container registry, pass the fully-qualified image name via the `image` parameter:

```csharp
var figApi = builder.AddFigApi("fig-api", image: "myregistry.example.com/myorg/fig-api");
var figWeb = builder.AddFigWeb("fig-web", image: "myregistry.example.com/myorg/fig-web")
    .WithFigApiReference(figApi);
```

The `tag` parameter continues to work as normal alongside a custom image:

```csharp
var figApi = builder.AddFigApi("fig-api", tag: "3.1.0", image: "myregistry.example.com/myorg/fig-api");
```

## Environment Variables

When using `WithReference(figApi)` on your project, the following environment variable is injected:

- `FIG_API_URI`: The HTTP URL of the Fig API endpoint

## More Information

- [Fig Documentation](https://www.figsettings.com)
- [Fig GitHub Repository](https://github.com/mzbrau/fig)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
