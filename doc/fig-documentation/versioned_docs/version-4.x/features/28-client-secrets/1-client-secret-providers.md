---
sidebar_position: 1
sidebar_label: Client Secrets
---

# Client Secrets

Fig uses a **client secret** to uniquely identify and authenticate each application instance that connects to the Fig configuration server. This secret is critical for securely associating configuration data and for protecting sensitive operations.

**Client secrets should be a GUID** (e.g., `b1a2c3d4-e5f6-7890-abcd-ef1234567890`) and must be the same across all running instances of an application. This ensures that all instances are recognized as the same logical client by the Fig server.

The secret is only known by the client and is passed to the API during registration, with the API storing a hash of the secret for the authentication of future requests.

## Purpose of the Client Secret

- **Authentication**: The client secret is used to authenticate the application with the Fig server.
- **Isolation**: Ensures that configuration and secrets are isolated per application instance.
- **Security**: Prevents unauthorized access to configuration data.

## How Client Secret Providers Are Used

When you configure Fig in your application, you specify a `ClientSecretProvider`. This provider is responsible for retrieving (and, in development, possibly creating) the client secret for your application. The secret is then used by the Fig client library to authenticate with the Fig server.

You can specify a client secret provider when configuring Fig:

```csharp
builder.Configuration.SetBasePath(GetBasePath())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "AspNetApi";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        options.ClientSecretProviders = [new DockerSecretProvider()];
    });
```


## Specifying Client Secrets Directly

For development or testing, you can specify the client secret directly using the `ClientSecretOverride` setting. This can be provided via command line, or hard-coded in your configuration options. **This approach is not recommended for production use.**

Example (hard-coded):

```csharp
options.ClientSecretOverride = "b1a2c3d4-e5f6-7890-abcd-ef1234567890";
```

You can also pass the client secret via command line for convenience in non-production scenarios.

## Provider Order and Overrides

The order of the `ClientSecretProviders` list is important: Fig will try each provider in order until one returns a valid secret. You can control the order by arranging the providers in the list. Additionally, you can set an environment variable to override the order in which providers are tried, allowing for flexible configuration across environments.

Add an environment variable called `FIG_CLIENT_SECRET_PROVIDERS` and list the names of the secret providers (`Docker`, `Dpapi`, `Azure`, `Aws`, `Google`) in a csv format with the first one being the one that should be tried first. Only specify providers that are supported by your application (or a subset of them).

## Available Client Secret Providers

Fig provides several built-in client secret providers for different environments and cloud platforms:

- [Docker Secret Provider](./2-docker-secret-provider.md): Uses Docker secrets mounted in `/run/secrets/` (Linux containers).
- [DPAPI Secret Provider](./3-dpapi-secret-provider.md): Uses Windows DPAPI or environment variables (Windows environments).
- [Azure Key Vault Secret Provider](./4-azure-secret-provider.md): Uses Azure Key Vault for secret storage and retrieval.
- [AWS Secrets Manager Provider](./5-aws-secret-provider.md): Uses AWS Secrets Manager for secret storage and retrieval.
- [Google Cloud Secret Manager Provider](./6-google-secret-provider.md): Uses Google Cloud Secret Manager for secret storage and retrieval.

Each provider has its own NuGet package, configuration, and platform requirements. See the linked documentation for details.

## Writing Your Own Client Secret Provider

You can implement your own client secret provider by inheriting from the `IClientSecretProvider` interface in the `Fig.Client.Contracts` package. Your provider must implement the logic to retrieve (and optionally create) a client secret for your application.

Example skeleton:

```csharp
using Fig.Client.Contracts;

public class MyCustomSecretProvider : IClientSecretProvider
{
    public Task<string> GetSecret(string clientName)
    {
        // Your logic to retrieve or create the secret
        return Task.FromResult("my-secret-value");
    }
}
```

You can then use your custom provider in the same way as the built-in providers.

## NuGet Package

The `Fig.Client.Contracts` project, which contains the `IClientSecretProvider` interface and base classes, is published as a NuGet package. Add it to your project:

```bash
dotnet add package Fig.Client.Contracts
```
