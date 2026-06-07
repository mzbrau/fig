using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Fig.Client.AppSettings;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal class DpapiValueProcessor : IDpapiValueProcessor
{
    public virtual bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public virtual string Encrypt(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public virtual string Decrypt(string cipherText)
    {
        var bytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
