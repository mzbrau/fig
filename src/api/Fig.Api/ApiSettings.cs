// ReSharper disable CollectionNeverUpdated.Global Set by appSettings.json

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Fig.Api;

public class ApiSettings
{
    private readonly Dictionary<string, string> _encryptionCache = new();
    private string? _decryptedSecret;
    
    public string Secret { get; set; } = null!;

    public long TokenLifeMinutes { get; set; }

    public string? PreviousSecret { get; set; }
    
    public bool SecretsDpapiEncrypted { get; set; }

    public List<string>? WebClientAddresses { get; set; }

    public bool ForceAdminDefaultPasswordChange { get; set; }
    public string DbConnectionString { get; set; }

    public string GetDecryptedSecret()
    {
        if (!SecretsDpapiEncrypted)
            return Secret;
        
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new ApplicationException("DPAPI secret resolution is only available on Windows");

        return _decryptedSecret ??= Encoding.UTF8
            .GetString(ProtectedData.Unprotect(Convert.FromBase64String(Secret), null,
                DataProtectionScope.CurrentUser));
    }

    public string? GetDecryptedPreviousSecret()
    {
        if (!SecretsDpapiEncrypted || string.IsNullOrWhiteSpace(PreviousSecret))
            return PreviousSecret;

        return DecryptUsingDpApi(PreviousSecret);
    }

    private string? DecryptUsingDpApi(string secret)
    {
        if (_encryptionCache.TryGetValue(secret, out var decryptedSecret))
            return decryptedSecret;
        
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new ApplicationException("DPAPI secret resolution is only available on Windows");

        var result = Encoding.UTF8
            .GetString(ProtectedData.Unprotect(Convert.FromBase64String(secret), null,
                DataProtectionScope.CurrentUser));

        _encryptionCache.Add(secret, result);

        return result;
    }
}