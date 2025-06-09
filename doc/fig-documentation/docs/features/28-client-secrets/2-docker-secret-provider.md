---
sidebar_position: 2
sidebar_label: Docker Client Secret Provider
---

# Fig Docker Secret Provider

This provider enables Fig client secret management using Docker secrets, designed for use in containerized Linux environments.

## Features

- **Linux-Only**: Automatically enabled on Linux containers.
- **Automatic Secret Creation**: Secrets are only auto-created if the environment variable `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is set to `Development`. In all other environments, secrets must already exist or a `SecretNotFoundException` will be thrown.
- **File-Based Storage**: Reads secrets from files mounted at `/run/secrets/`.
- **Thread-Safe**: Safe for concurrent use.
- **Graceful Error Handling**: Handles missing files and permission errors with clear exceptions.

## Installation

```bash
dotnet add package Fig.Client.SecretProvider.Docker
```

## Usage

### Basic Usage

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

## Secret Naming Convention

Secrets are stored as files in `/run/secrets/` using the format: `FIG_{CLIENT_NAME}_SECRET` (uppercase, no spaces).  
For example, for client name `MyService`, the secret file will be `/run/secrets/FIG_MYSERVICE_SECRET` or `/run/secrets/FIG_MYSERVICE_SECRET.txt`.

## How It Works

- The provider attempts to read the secret from `/run/secrets/FIG_{CLIENT_NAME}_SECRET` or `/run/secrets/FIG_{CLIENT_NAME}_SECRET.txt`.
- If the secret file does not exist and the environment is `Development`, it will attempt to create a new secret file with a random GUID.
- If the file cannot be found and the environment is not `Development`, a `SecretNotFoundException` is thrown.
- If the provider cannot create the file due to permissions or missing directory, an exception is thrown.

## Best Practices

1. **Mount Docker Secrets**: Use Docker's secrets mechanism to mount secrets at `/run/secrets/`.
2. **Least Privilege**: Run containers with only the permissions needed to read secrets.
3. **No AutoCreate in Production**: In production and all non-development environments, secrets will not be auto-created.
4. **Monitor for Errors**: Log and monitor for missing secret or permission errors.

## Example: Mounting a Secret in Docker

```bash
echo "my-secret-value" | docker secret create FIG_MYSERVICE_SECRET -
docker service create --name myservice --secret FIG_MYSERVICE_SECRET myimage
```

The secret will be available to the container at `/run/secrets/FIG_MYSERVICE_SECRET`.
