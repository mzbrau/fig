---
sidebar_position: 3
sidebar_label: DPAPI
---

# Fig DPAPI Secret Provider

This provider enables Fig client secret management using Windows DPAPI (Data Protection API), designed for use on Windows systems.

## Features

- **Windows-Only**: Automatically enabled on Windows platforms.
- **Automatic Secret Creation**: Secrets are auto-created if not present, unless auto-creation is disabled.
- **Machine-Scoped Environment Variable Storage**: Auto-created secrets are stored as Machine environment variables when the process can write them (typically elevated). Non-elevated auto-create falls back to User scope with a warning.
- **User-Scoped Fallback**: If no machine-scoped secret exists, the provider can read a user-scoped environment variable (legacy / non-elevated installs) and warns that Machine scope is preferred.
- **LocalMachine DPAPI**: New secrets are encrypted with `DataProtectionScope.LocalMachine`. Legacy CurrentUser ciphertext is still decrypted with a warning.
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

- The provider attempts to read the secret from the **machine-scoped** environment variable `FIG_{CLIENT_NAME}_SECRET`.
- If the variable exists, it is decrypted using DPAPI (`LocalMachine` first, then legacy `CurrentUser` with a warning).
- If no machine-scoped value exists, the provider attempts to read the **user-scoped** environment variable with the same name, logs a warning that Machine scope is preferred (required for Local System), and best-effort copies the value to Machine when elevated.
- If the variable does not exist and auto-creation is enabled, a new GUID is generated, encrypted with `LocalMachine`, and persisted to the Machine environment when possible. If the Machine write fails (not elevated), the secret is stored in the User environment with a warning.
- If the variable cannot be found and auto-creation is disabled, a `SecretNotFoundException` is thrown.
- If decryption fails (e.g., corruption or ciphertext from another machine), a `SecretNotFoundException` is thrown with instructions for manual creation.

## Best Practices

1. **Set Machine Environment Variables**: For production and Windows services (including Local System), set the encrypted value as a Machine environment variable via an elevated process, Install-Fig, or admin PowerShell.
2. **Same Machine**: Encrypt on the machine that will decrypt. LocalMachine DPAPI does not transfer across machines.
3. **No AutoCreate in Production**: For production, pre-set the environment variable and disable auto-creation.
4. **Monitor for Warnings**: Watch logs for CurrentUser crypto fallback or User-scoped env warnings and migrate promptly.
5. **Threat model**: Any process on the machine that can read the ciphertext can decrypt LocalMachine DPAPI values. Use ACLs and prefer cloud/Docker secret providers when stronger isolation is required.

## Example: Creating a DPAPI Secret via PowerShell

Run elevated so the Machine environment variable can be set:

```powershell
$scope = [System.Security.Cryptography.DataProtectionScope]::LocalMachine
$secret = [System.Text.Encoding]::UTF8.GetBytes("<YOUR CLIENT SECRET HERE>")
$protected = [System.Security.Cryptography.ProtectedData]::Protect($secret, $null, $scope)
$encodedText = [Convert]::ToBase64String($protected)
Write-Host $encodedText
# Persist the environment variable for the machine (requires elevation)
[System.Environment]::SetEnvironmentVariable("FIG_MYSERVICE_SECRET", $encodedText, "Machine")
```

You can also use the `dpapi` tool available from the Fig repository to generate and set secrets. See [Encrypting Secrets DPAPI](../../guides/3-encrypting-secrets-dpapi.md) for details.
