---
sidebar_position: 13
---

# NuGet Packages

Fig provides several NuGet packages for different integration scenarios.

## Core

### [Fig.Client](https://www.nuget.org/packages/Fig.Client)

The main client library. Add this to applications whose settings Fig should manage.

- **Target framework**: .NET Standard 2.0
- **Documentation**: [Client Configuration](./client-configuration.md)

### [Fig.Client.Abstractions](https://www.nuget.org/packages/Fig.Client.Abstractions)

Attributes and contracts so third-party libraries can declare Fig settings without taking a dependency on the full client.

- **Target framework**: .NET Standard 2.0

### [Fig.Client.Contracts](https://www.nuget.org/packages/Fig.Client.Contracts)

Internal contracts, including `IClientSecretProvider`. Most applications do not reference this directly unless they write a custom secret provider.

### [Fig.WebHooks.Contracts](https://www.nuget.org/packages/Fig.WebHooks.Contracts)

Data contracts for webhook payloads. Use this when building a webhook integration.

## Secret providers

Each provider is a separate package. See [Client Secrets](./features/28-client-secrets/1-client-secret-providers.md).

| Package | Documentation |
| ------- | ------------- |
| [Fig.Client.SecretProvider.Docker](https://www.nuget.org/packages/Fig.Client.SecretProvider.Docker) | [Docker](./features/28-client-secrets/2-docker-secret-provider.md) |
| [Fig.Client.SecretProvider.Dpapi](https://www.nuget.org/packages/Fig.Client.SecretProvider.Dpapi) | [DPAPI](./features/28-client-secrets/3-dpapi-secret-provider.md) (Windows) |
| [Fig.Client.SecretProvider.Azure](https://www.nuget.org/packages/Fig.Client.SecretProvider.Azure) | [Azure Key Vault client secret](./features/28-client-secrets/4-azure-secret-provider.md) |
| [Fig.Client.SecretProvider.Aws](https://www.nuget.org/packages/Fig.Client.SecretProvider.Aws) | [AWS Secrets Manager](./features/28-client-secrets/5-aws-secret-provider.md) |
| [Fig.Client.SecretProvider.Google](https://www.nuget.org/packages/Fig.Client.SecretProvider.Google) | [Google Cloud Secret Manager](./features/28-client-secrets/6-google-secret-provider.md) |

Azure Key Vault can also store **setting values** on the API (a different feature). See [Azure Key Vault Integration](./features/26-azure-keyvault-integration.md).

## Testing and hosting

### [Fig.Client.Testing](https://www.nuget.org/packages/Fig.Client.Testing)

Reloadable configuration for integration tests, plus display-script testing helpers. See [Integration Testing](./guides/4-integration-testing.md) and [Settings Binding Verification](./guides/10-settings-binding-verification.md).

### [Fig.Aspire](https://www.nuget.org/packages/Fig.Aspire)

AppHost extension methods (`AddFigApi`, `AddFigWeb`). See [.NET Aspire Integration](./guides/9-aspire-integration.md).

## Not a NuGet package

**Fig MCP** is a standalone application / container that proxies MCP tools to the Fig API. See [Fig MCP Server](./integrations/fig-mcp-server.md).

## Installation examples

```bash
dotnet add package Fig.Client
dotnet add package Fig.Client.SecretProvider.Azure
dotnet add package Fig.Client.Testing
```
