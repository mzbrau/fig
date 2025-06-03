---
sidebar_position: 6
sidebar_label: Google Cloud Client Secret Provider
---

# Fig Google Cloud Secret Provider

:::warning[Experimental]

This integration has not been well tested and may contain bugs. Please report any bugs to the github repo

:::

This package provides Google Cloud Secret Manager integration for Fig client secret management using managed identity authentication.

## Features

- **Managed Identity Support**: Uses Google Cloud's Application Default Credentials (service accounts, workload identity, etc.)
- **Automatic Secret Creation**: Secrets are only auto-created if the environment variable `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is set to `Development`. In all other environments, secrets must already exist or a `SecretNotFoundException` will be thrown.
- **Thread-Safe**: Safe for concurrent use across multiple threads
- **Retry Logic**: Built-in exponential backoff for transient errors
- **Resource Management**: Implements IDisposable for proper cleanup
- **Google Cloud Best Practices**: Follows recommended naming conventions and security practices

## Installation

```bash
dotnet add package Fig.Client.SecretProvider.Google
```

## Usage

### Basic Usage with Default Credentials

```csharp
using Fig.Client.Google;

builder.Configuration.SetBasePath(GetBasePath())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "AspNetApi";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        options.ClientSecretProviders = [new GoogleSecretProvider("your-project-id")]
    });
```

## Secret Naming Convention

Secrets are stored in Google Cloud Secret Manager using the format: `FIG_{CLIENT_NAME}_SECRET`

For example, if your client name is "MyService", the secret will be named: `FIG_MYSERVICE_SECRET`

**Note**: This follows the same naming convention as the Azure and AWS secret providers for consistency across cloud platforms.

## Authentication

This provider uses Google Cloud's Application Default Credentials (ADC), which checks for credentials in the following order:

1. `GOOGLE_APPLICATION_CREDENTIALS` environment variable pointing to a service account key file
2. Google Cloud SDK credentials (`gcloud auth application-default login`)
3. Google Cloud Shell credentials
4. Compute Engine service account (when running on Google Cloud)
5. Google Kubernetes Engine workload identity (when running on GKE)

## Required Google Cloud Permissions

The service account or user must have the following IAM permissions:

```json
{
  "bindings": [
    {
      "role": "roles/secretmanager.secretAccessor",
      "members": ["serviceAccount:your-service-account@project.iam.gserviceaccount.com"]
    },
    {
      "role": "roles/secretmanager.secretCreator",
      "members": ["serviceAccount:your-service-account@project.iam.gserviceaccount.com"]
    }
  ]
}
```

### Minimal Custom Role

For more granular control, create a custom role with these permissions:

```json
{
  "title": "Fig Secret Manager",
  "description": "Minimal permissions for Fig secret provider",
  "permissions": [
    "secretmanager.secrets.create",
    "secretmanager.secrets.get",
    "secretmanager.versions.access",
    "secretmanager.versions.add"
  ]
}
```

In non-development environments, secrets are never auto-created, so you only need:

- `secretmanager.secrets.get`
- `secretmanager.versions.access`

## Error Handling

The provider handles common Google Cloud errors gracefully:

- **NotFound**: Thrown when a secret doesn't exist and the environment is not `Development`
- **Transient Errors**: Automatically retried with exponential backoff (rate limiting, service unavailable, etc.)
- **Concurrent Creation**: Handles race conditions when multiple instances try to create the same secret

## Best Practices

1. **Use Workload Identity**: When running on Google Kubernetes Engine, use workload identity
2. **Service Accounts**: Use dedicated service accounts with minimal permissions
3. **Least Privilege**: Grant only the minimum required permissions
4. **Resource Cleanup**: Dispose the provider when your application shuts down
5. **Error Monitoring**: Log and monitor for authentication and permission errors
6. **Secret Rotation**: Consider implementing secret rotation policies
7. **No AutoCreate in Production**: In production and all non-development environments, secrets will not be auto-created.

## Google Cloud Setup

### Enable the Secret Manager API

```bash
gcloud services enable secretmanager.googleapis.com
```

### Create a Service Account (if needed)

```bash
gcloud iam service-accounts create fig-secret-manager \
    --display-name="Fig Secret Manager" \
    --description="Service account for Fig secret management"
```

### Grant Permissions

```bash
gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
    --member="serviceAccount:fig-secret-manager@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/secretmanager.secretAccessor"

gcloud projects add-iam-policy-binding YOUR_PROJECT_ID \
    --member="serviceAccount:fig-secret-manager@YOUR_PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/secretmanager.secretCreator"
```

## Thread Safety

The `GoogleSecretProvider` is thread-safe and uses a semaphore to prevent race conditions during secret creation. Multiple threads can safely call `GetSecret()` concurrently.

## Integration with Fig

This provider implements the `IClientSecretProvider` interface and can be used with any Fig configuration:

```csharp
var secretProvider = new GoogleSecretProvider("your-project-id");

var config = FigConfigurationBuilder.Start<MySettings>()
    .WithClientSecretProvider(secretProvider)
    .WithApiUris("https://your-fig-api.com")
    .Build();
```

## Troubleshooting

### Common Issues

1. **Permission Denied**: Ensure your service account has the required permissions
2. **Project Not Found**: Verify the project ID is correct and accessible
3. **API Not Enabled**: Enable the Secret Manager API in your Google Cloud project
4. **Authentication Failed**: Check your Application Default Credentials setup

### Enable Logging

Enable Google Cloud client library logging for troubleshooting:

```csharp
// In your startup code
Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "DEBUG");
Environment.SetEnvironmentVariable("GRPC_TRACE", "all");
```

### Verify Credentials

Test your credentials using the Google Cloud SDK:

```bash
gcloud auth application-default print-access-token
```

## Environment Variables

- `GOOGLE_APPLICATION_CREDENTIALS`: Path to service account key file
- `GOOGLE_CLOUD_PROJECT`: Default project ID (optional)
- `GRPC_VERBOSITY`: Set to `DEBUG` for verbose logging
- `GRPC_TRACE`: Set to `all` for detailed gRPC tracing

## Regional Considerations

Google Cloud Secret Manager automatically replicates secrets globally by default. For specific regional requirements, you can configure replication policies when creating secrets manually or extend the provider to support custom replication settings.
