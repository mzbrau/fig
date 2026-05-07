using System.Diagnostics.CodeAnalysis;
using Fig.Client.Contracts;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Fig.Client.SecretProvider.Dpapi
{
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public class DpapiSecretProvider : ClientSecretProviderBase<DpapiSecretProvider>
    {
        public DpapiSecretProvider()
            : this(null)
        {
        }

        protected DpapiSecretProvider(bool? autoCreate)
            : base("Dpapi", autoCreate)
        {
        }

        public override bool IsEnabled => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        protected override Task<string> GetOrCreateSecretInternal(string clientName)
        {
            var secretKey = string.Format(SecretKeyFormat, clientName.Replace(" ", "").ToUpper());
            var encryptedSecret = GetStoredEncryptedSecret(secretKey);

            Logger?.LogDebug("Attempting to retrieve DPAPI secret for key {SecretKey} (client: {ClientName})",
                secretKey, clientName);
            if (!string.IsNullOrEmpty(encryptedSecret))
            {
                return Task.FromResult(ReadStoredSecret(clientName, secretKey, encryptedSecret, "user-scoped"));
            }

            var machineScopedSecret = GetCurrentProcessEncryptedSecret(secretKey);
            if (!string.IsNullOrEmpty(machineScopedSecret))
            {
                var secret = ReadStoredSecret(clientName, secretKey, machineScopedSecret, "machine-scoped");

                try
                {
                    SetStoredEncryptedSecret(secretKey, machineScopedSecret);
                    Logger?.LogInformation(
                        "Migrated DPAPI secret for key {SecretKey} from machine-scoped to user-scoped environment variable (client: {ClientName})",
                        secretKey,
                        clientName);
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(
                        "Unable to persist machine-scoped DPAPI secret to user scope for key {SecretKey} (client: {ClientName}). Continuing with machine-scoped secret. Error: {Error}",
                        secretKey,
                        clientName,
                        ex.Message);
                }

                return Task.FromResult(secret);
            }

            if (!AutoCreate)
            {
                Logger?.LogError(
                    "Encrypted client secret must be set in a user or machine environment variable called {SecretKey} (client: {ClientName})",
                    secretKey, clientName);
                throw new SecretNotFoundException(
                    $"Encrypted client secret must be set in a user or machine environment variable called {secretKey}");
            }

            var newSecret = Guid.NewGuid().ToString();
            var protectedSecret = Protect(newSecret, DataProtectionScope.CurrentUser);
            try
            {
                SetStoredEncryptedSecret(secretKey, protectedSecret);
                var verifyEncrypted = GetStoredEncryptedSecret(secretKey);
                if (!string.IsNullOrEmpty(verifyEncrypted))
                {
                    var verifySecret = Unprotect(verifyEncrypted, DataProtectionScope.CurrentUser);
                    if (verifySecret == newSecret)
                    {
                        SecretCache[clientName] = newSecret;
                        Logger?.LogInformation(
                            "Successfully created DPAPI secret for key {SecretKey} (client: {ClientName})", secretKey,
                            clientName);
                        return Task.FromResult(newSecret);
                    }
                }

                if (!string.IsNullOrEmpty(verifyEncrypted))
                {
                    Logger?.LogWarning(
                        "DPAPI secret {SecretKey} was created concurrently by another process. Using existing value",
                        secretKey);
                    var verifySecret = Unprotect(verifyEncrypted, DataProtectionScope.CurrentUser);
                    SecretCache[clientName] = verifySecret;
                    return Task.FromResult(verifySecret);
                }

                Logger?.LogError("Failed to create or verify DPAPI secret in environment variable {SecretKey}",
                    secretKey);
                throw new InvalidOperationException(
                    $"Failed to create or verify DPAPI secret in environment variable '{secretKey}'");
            }
            catch (Exception ex)
            {
                Logger?.LogError("Error creating or reading DPAPI secret for {ClientName}: {Error}", clientName,
                    ex.Message);
                throw new InvalidOperationException(
                    $"Error creating or reading DPAPI secret for '{clientName}': {ex.Message}", ex);
            }
        }

        protected virtual string? GetStoredEncryptedSecret(string secretKey)
        {
            return Environment.GetEnvironmentVariable(secretKey, EnvironmentVariableTarget.User);
        }

        protected virtual string? GetCurrentProcessEncryptedSecret(string secretKey)
        {
            return Environment.GetEnvironmentVariable(secretKey, EnvironmentVariableTarget.Machine);
        }

        protected virtual void SetStoredEncryptedSecret(string secretKey, string encryptedSecret)
        {
            Environment.SetEnvironmentVariable(secretKey, encryptedSecret, EnvironmentVariableTarget.User);
        }

        private string ReadStoredSecret(string clientName, string secretKey, string encryptedSecret, string scopeDescription)
        {
            try
            {
                var secret = Unprotect(encryptedSecret, DataProtectionScope.CurrentUser);
                SecretCache[clientName] = secret;
                Logger?.LogInformation(
                    "Successfully retrieved DPAPI secret for key {SecretKey} from the {ScopeDescription} environment variable (client: {ClientName})",
                    secretKey,
                    scopeDescription,
                    clientName);
                return secret;
            }
            catch (Exception ex)
            {
                Logger?.LogError(
                    "Failed to decrypt DPAPI secret from the {ScopeDescription} environment variable {SecretKey}: {Error}",
                    scopeDescription,
                    secretKey,
                    ex.Message);
                throw new SecretNotFoundException(
                    $"Failed to decrypt DPAPI secret from the {scopeDescription} environment variable '{secretKey}': {ex.Message}",
                    ex);
            }
        }

        protected virtual string Unprotect(string encryptedString, DataProtectionScope scope)
        {
            try
            {
                return Encoding.UTF8
                    .GetString(ProtectedData.Unprotect(Convert.FromBase64String(encryptedString), null, scope));
            }
            catch (Exception)
            {
                throw new SecretNotFoundException(
                    $"Invalid DPAPI encrypted value {encryptedString}. Client secret can be added via PowerShell (core) " +
                    $"running as the same user as your application ({Environment.UserName}), using the following commands:" +
                    "\r$scope = [System.Security.Cryptography.DataProtectionScope]::CurrentUser" +
                    "\r$secret = [System.Text.Encoding]::UTF8.GetBytes(\"<YOUR CLIENT SECRET HERE>\")" +
                    "\r$protected = [System.Security.Cryptography.ProtectedData]::Protect($secret, $null, $scope)" +
                    "\r$encodedText = [Convert]::ToBase64String($protected)" +
                    "\rWrite-Host $encodedText" +
                    "\r It can also be set using the dpapi tool available from the Fig repository.");
            }
        }

        protected virtual string Protect(string plainText, DataProtectionScope scope)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(plainText);
                var protectedBytes = ProtectedData.Protect(bytes, null, scope);
                return Convert.ToBase64String(protectedBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to encrypt secret for DPAPI: {ex.Message}", ex);
            }
        }
    }
}
