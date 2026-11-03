using System.Diagnostics.CodeAnalysis;
using Fig.Client.Contracts;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Fig.Client.SecretProvider.Dpapi
{
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public class DpapiSecretProvider : ClientSecretProviderBase<DpapiSecretProvider>, IAppSettingsEncryptionProvider
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

        // IAppSettingsEncryptionProvider — allows DpapiSecretProvider to be detected automatically
        // by FigConfigurationBuilder for --printappsettings and --figoffline without extra registration.
        bool IAppSettingsEncryptionProvider.IsSupported => IsEnabled;

        string IAppSettingsEncryptionProvider.Encrypt(string plainText) =>
            Protect(plainText, DataProtectionScope.LocalMachine);

        string IAppSettingsEncryptionProvider.Decrypt(string cipherText) =>
            UnprotectWithFallback(cipherText);

        protected override Task<string> GetOrCreateSecretInternal(string clientName)
        {
            var secretKey = string.Format(SecretKeyFormat, clientName.Replace(" ", "").ToUpper());

            Logger?.LogDebug("Attempting to retrieve DPAPI secret for key {SecretKey} (client: {ClientName})",
                secretKey, clientName);

            var machineLookup = TryReadMachineScopedSecret(clientName, secretKey);
            if (machineLookup.Status == MachineLookupStatus.Decrypted)
            {
                return Task.FromResult(machineLookup.Secret!);
            }

            var userScopedSecret = TryGetUserScopedEncryptedSecret(secretKey);
            if (userScopedSecret is { Length: > 0 })
            {
                var secret = ReadStoredSecret(clientName, secretKey, userScopedSecret, "user-scoped");

                Logger?.LogWarning(
                    "DPAPI secret for key {SecretKey} was read from a user-scoped environment variable (client: {ClientName}). " +
                    "Machine-scoped environment variables are preferred so Windows services (including Local System) can resolve the secret. " +
                    "Move the value to a Machine environment variable using an elevated process, Install-Fig, or the DPAPI tool.",
                    secretKey,
                    clientName);

                // Only migrate when Machine is absent. If Machine holds unreadable ciphertext
                // (e.g. another account's CurrentUser blob), leave it untouched.
                if (machineLookup.Status == MachineLookupStatus.Absent)
                {
                    try
                    {
                        // Re-protect with LocalMachine before writing Machine env — copying CurrentUser
                        // ciphertext would leave Local System unable to decrypt it.
                        var machineProtected = Protect(secret, DataProtectionScope.LocalMachine);
                        SetMachineScopedEncryptedSecret(secretKey, machineProtected);
                        Logger?.LogInformation(
                            "Migrated DPAPI secret for key {SecretKey} from user-scoped to machine-scoped environment variable (client: {ClientName})",
                            secretKey,
                            clientName);
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogWarning(
                            "Unable to persist user-scoped DPAPI secret to machine scope for key {SecretKey} (client: {ClientName}). " +
                            "Continuing with user-scoped secret. Machine writes typically require elevation. Error: {Error}",
                            secretKey,
                            clientName,
                            ex.Message);
                    }
                }

                return Task.FromResult(secret);
            }

            if (!AutoCreate)
            {
                Logger?.LogError(
                    "Encrypted client secret must be set in a machine or user environment variable called {SecretKey} (client: {ClientName})",
                    secretKey, clientName);
                throw new SecretNotFoundException(
                    $"Encrypted client secret must be set in a machine or user environment variable called {secretKey}");
            }

            var newSecret = Guid.NewGuid().ToString();
            var protectedSecret = Protect(newSecret, DataProtectionScope.LocalMachine);
            try
            {
                var storedTarget = PersistNewSecret(secretKey, protectedSecret, clientName);
                var verifyEncrypted = storedTarget == EnvironmentVariableTarget.Machine
                    ? GetMachineScopedEncryptedSecret(secretKey)
                    : GetUserScopedEncryptedSecret(secretKey);

                if (verifyEncrypted is { Length: > 0 })
                {
                    var verifySecret = UnprotectWithFallback(verifyEncrypted);
                    if (verifySecret == newSecret)
                    {
                        SecretCache[clientName] = newSecret;
                        Logger?.LogInformation(
                            "Successfully created DPAPI secret for key {SecretKey} in {Target}-scoped environment variable (client: {ClientName})",
                            secretKey,
                            storedTarget,
                            clientName);
                        return Task.FromResult(newSecret);
                    }

                    Logger?.LogWarning(
                        "DPAPI secret {SecretKey} was created concurrently by another process. Using existing value",
                        secretKey);
                    SecretCache[clientName] = verifySecret;
                    return Task.FromResult(verifySecret);
                }

                Logger?.LogError("Failed to create or verify DPAPI secret in environment variable {SecretKey}",
                    secretKey);
                throw new InvalidOperationException(
                    $"Failed to create or verify DPAPI secret in environment variable '{secretKey}'");
            }
            catch (SecretNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger?.LogError("Error creating or reading DPAPI secret for {ClientName}: {Error}", clientName,
                    ex.Message);
                throw new InvalidOperationException(
                    $"Error creating or reading DPAPI secret for '{clientName}': {ex.Message}", ex);
            }
        }

        private enum MachineLookupStatus
        {
            Absent,
            Decrypted,
            Unreadable
        }

        private sealed class MachineLookupResult
        {
            public MachineLookupResult(MachineLookupStatus status, string? secret)
            {
                Status = status;
                Secret = secret;
            }

            public MachineLookupStatus Status { get; }

            public string? Secret { get; }
        }

        private MachineLookupResult TryReadMachineScopedSecret(string clientName, string secretKey)
        {
            string? machineScopedSecret;
            try
            {
                machineScopedSecret = GetMachineScopedEncryptedSecret(secretKey);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(
                    "Failed to read machine-scoped DPAPI secret for key {SecretKey} (client: {ClientName}). " +
                    "Falling back to user-scoped lookup. Error: {Error}",
                    secretKey,
                    clientName,
                    ex.Message);
                return new MachineLookupResult(MachineLookupStatus.Unreadable, null);
            }

            if (machineScopedSecret is not { Length: > 0 })
                return new MachineLookupResult(MachineLookupStatus.Absent, null);

            try
            {
                var secret = ReadStoredSecret(clientName, secretKey, machineScopedSecret, "machine-scoped");
                return new MachineLookupResult(MachineLookupStatus.Decrypted, secret);
            }
            catch (Exception ex)
            {
                // Leave the unreadable Machine value untouched so we do not overwrite another
                // account's ciphertext or auto-create a replacement while a User value may exist.
                Logger?.LogWarning(
                    "Failed to decrypt machine-scoped DPAPI secret for key {SecretKey} (client: {ClientName}). " +
                    "Falling back to user-scoped lookup without modifying the Machine value. Error: {Error}",
                    secretKey,
                    clientName,
                    ex.Message);
                return new MachineLookupResult(MachineLookupStatus.Unreadable, null);
            }
        }

        private string? TryGetUserScopedEncryptedSecret(string secretKey)
        {
            try
            {
                return GetUserScopedEncryptedSecret(secretKey);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(
                    "Failed to read user-scoped DPAPI secret for key {SecretKey}. Error: {Error}",
                    secretKey,
                    ex.Message);
                return null;
            }
        }

        private EnvironmentVariableTarget PersistNewSecret(string secretKey, string protectedSecret, string clientName)
        {
            try
            {
                SetMachineScopedEncryptedSecret(secretKey, protectedSecret);
                return EnvironmentVariableTarget.Machine;
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(
                    "Unable to store DPAPI secret for key {SecretKey} in a machine-scoped environment variable (client: {ClientName}). " +
                    "Falling back to user scope because Machine writes typically require elevation. " +
                    "For Windows services (including Local System), re-run elevated or set the Machine environment variable via Install-Fig, admin PowerShell, or the DPAPI tool. Error: {Error}",
                    secretKey,
                    clientName,
                    ex.Message);

                SetUserScopedEncryptedSecret(secretKey, protectedSecret);
                return EnvironmentVariableTarget.User;
            }
        }

        protected virtual string? GetUserScopedEncryptedSecret(string secretKey)
        {
            return Environment.GetEnvironmentVariable(secretKey, EnvironmentVariableTarget.User);
        }

        protected virtual string? GetMachineScopedEncryptedSecret(string secretKey)
        {
            return Environment.GetEnvironmentVariable(secretKey, EnvironmentVariableTarget.Machine);
        }

        protected virtual void SetUserScopedEncryptedSecret(string secretKey, string encryptedSecret)
        {
            Environment.SetEnvironmentVariable(secretKey, encryptedSecret, EnvironmentVariableTarget.User);
        }

        protected virtual void SetMachineScopedEncryptedSecret(string secretKey, string encryptedSecret)
        {
            Environment.SetEnvironmentVariable(secretKey, encryptedSecret, EnvironmentVariableTarget.Machine);
        }

        [Obsolete("Use GetUserScopedEncryptedSecret instead. Will be removed in a future release.")]
        protected virtual string? GetStoredEncryptedSecret(string secretKey) =>
            GetUserScopedEncryptedSecret(secretKey);

        [Obsolete("Use GetMachineScopedEncryptedSecret instead. Will be removed in a future release.")]
        protected virtual string? GetCurrentProcessEncryptedSecret(string secretKey) =>
            GetMachineScopedEncryptedSecret(secretKey);

        [Obsolete("Use SetUserScopedEncryptedSecret instead. Will be removed in a future release.")]
        protected virtual void SetStoredEncryptedSecret(string secretKey, string encryptedSecret) =>
            SetUserScopedEncryptedSecret(secretKey, encryptedSecret);

        private string ReadStoredSecret(string clientName, string secretKey, string encryptedSecret, string scopeDescription)
        {
            try
            {
                var secret = UnprotectWithFallback(encryptedSecret);
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

        private string UnprotectWithFallback(string encryptedString)
        {
            // Note: on Windows, CryptUnprotectData ignores CRYPTPROTECT_LOCAL_MACHINE during
            // unprotect, so CurrentUser ciphertext owned by the running account may succeed on
            // the LocalMachine attempt and this catch may not run. User-env warnings plus
            // LocalMachine re-protect on User→Machine migration are the primary migration signals.
            try
            {
                return Unprotect(encryptedString, DataProtectionScope.LocalMachine);
            }
            catch (SecretNotFoundException)
            {
                var plaintext = Unprotect(encryptedString, DataProtectionScope.CurrentUser);
                Logger?.LogWarning(
                    "DPAPI secret was decrypted using CurrentUser scope. Re-encrypt it with LocalMachine scope " +
                    "(updated DPAPI tool or PowerShell DataProtectionScope.LocalMachine) so Windows services " +
                    "including Local System can resolve the secret. CurrentUser fallback may be removed in a future release.");
                return plaintext;
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
                    $"Invalid DPAPI encrypted value {encryptedString}. Client secret can be added via elevated PowerShell (core) " +
                    "on the target machine using the following commands:" +
                    "\r$scope = [System.Security.Cryptography.DataProtectionScope]::LocalMachine" +
                    "\r$secret = [System.Text.Encoding]::UTF8.GetBytes(\"<YOUR CLIENT SECRET HERE>\")" +
                    "\r$protected = [System.Security.Cryptography.ProtectedData]::Protect($secret, $null, $scope)" +
                    "\r$encodedText = [Convert]::ToBase64String($protected)" +
                    "\rWrite-Host $encodedText" +
                    "\r[System.Environment]::SetEnvironmentVariable(\"FIG_YOURCLIENT_SECRET\", $encodedText, \"Machine\")" +
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
