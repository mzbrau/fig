---
sidebar_position: 3
sidebar_label: Default Client Secret Provider
---

# Fig DPAPI Secret Provider

This provider enables Fig client secret management using Windows DPAPI (Data Protection API), designed for use on Windows systems.

## Features

- **Windows-Only**: Automatically enabled on Windows platforms.
- **Automatic Secret Creation**: Secrets are auto-created if not present, unless auto-creation is disabled.
- **Environment Variable Storage**: Stores secrets as encrypted environment variables, using DPAPI for encryption.
- **Thread-Safe**: Safe for concurrent use.
- **Graceful Error Handling**: Handles decryption and environment errors with clear exceptions.

## Installation

```bash
dotnet add package Fig.Client.SecretProvider.Dpapi
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
        options.ClientSecretProviders = [new DpapiSecretProvider()];
    });
```

## Secret Naming Convention

Secrets are stored as environment variables using the format: `FIG_{CLIENT_NAME}_SECRET` (uppercase, no spaces).
For example, for client name `MyService`, the environment variable will be `FIG_MYSERVICE_SECRET`.

## How It Works

- The provider attempts to read the secret from the environment variable `FIG_{CLIENT_NAME}_SECRET`.
- If the variable exists, it is decrypted using DPAPI (CurrentUser scope).
- If the variable does not exist and auto-creation is enabled, a new GUID is generated, encrypted, and set as the environment variable for the current process.
- If the variable cannot be found and auto-creation is disabled, a `SecretNotFoundException` is thrown.
- If decryption fails (e.g., due to user mismatch or corruption), a `SecretNotFoundException` is thrown with instructions for manual creation.

## Best Practices

1. **Set Environment Variables Securely**: Use deployment scripts or CI/CD pipelines to set the encrypted environment variable for the correct user.
2. **User Context**: Ensure the application and the tool used to encrypt the secret run as the same Windows user.
3. **No AutoCreate in Production**: For production, pre-set the environment variable and disable auto-creation.
4. **Monitor for Errors**: Log and monitor for missing or decryption errors.

## Example: Creating a DPAPI Secret via PowerShell

```powershell
$scope = [System.Security.Cryptography.DataProtectionScope]::CurrentUser
$secret = [System.Text.Encoding]::UTF8.GetBytes("<YOUR CLIENT SECRET HERE>")
$protected = [System.Security.Cryptography.ProtectedData]::Protect($secret, $null, $scope)
$encodedText = [Convert]::ToBase64String($protected)
Write-Host $encodedText
# Set the environment variable (for current process)
[System.Environment]::SetEnvironmentVariable("FIG_MYSERVICE_SECRET", $encodedText, "User")
```

You can also use the `dpapi` tool available from the Fig repository to generate and set secrets. See [Encrypting Secrets DPAPI](../../guides/3-encrypting-secrets-dpapi.md) for details.
