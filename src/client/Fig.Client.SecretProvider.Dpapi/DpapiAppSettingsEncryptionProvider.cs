using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Fig.Client.Contracts;

namespace Fig.Client.SecretProvider.Dpapi;

/// <summary>
/// Encrypts and decrypts appsettings.json secret values using Windows DPAPI
/// (Data Protection API) scoped to the current user.
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class DpapiAppSettingsEncryptionProvider : IAppSettingsEncryptionProvider
{
    public string Name => "Dpapi";

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public virtual string Encrypt(string plainText)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to DPAPI-encrypt value: {ex.Message}", ex);
        }
    }

    public virtual string Decrypt(string cipherText)
    {
        try
        {
            var bytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to DPAPI-decrypt value: {ex.Message}", ex);
        }
    }
}
