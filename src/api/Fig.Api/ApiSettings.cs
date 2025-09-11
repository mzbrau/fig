// ReSharper disable CollectionNeverUpdated.Global Set by appSettings.json

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;

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
    
    public required string DbConnectionString { get; set; }
    
    public long SchedulingCheckIntervalMs { get; set; }
    
    public long TimeMachineCheckIntervalMs { get; set; }

    public RateLimitingSettings? RateLimiting { get; set; }

    // When true, enables ASP.NET Core ForwardedHeaders middleware with
    // trusted proxies/networks to populate Connection.RemoteIpAddress safely.
    public bool TrustForwardedHeaders { get; set; } = false;

    // List of proxy IP addresses that are trusted to supply forwarded headers.
    public List<string>? KnownProxies { get; set; }

    // List of CIDR network ranges (e.g., "10.0.0.0/8") that are trusted
    // to supply forwarded headers.
    public List<string>? KnownNetworks { get; set; }

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

public class RateLimitingSettings
{
    public GlobalPolicySettings GlobalPolicy { get; set; } = new();
}

public class GlobalPolicySettings
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 500;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    public QueueProcessingOrder ProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;
    public int QueueLimit { get; set; } = 10;
}