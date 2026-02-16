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
    
    public bool DisableTransactionMiddleware { get; set; }

    public AuthenticationSettings Authentication { get; set; } = new();

    public string? ImportFolderPath { get; set; }
    
    /// <summary>
    /// Cache expiry time in minutes for hash validation results.
    /// Default is 60 minutes. Set to 0 to disable caching.
    /// </summary>
    public int HashCacheExpiryMinutes { get; set; } = 60;

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

public class AuthenticationSettings
{
    public AuthMode Mode { get; set; } = AuthMode.FigManaged;

    public KeycloakAuthenticationSettings Keycloak { get; set; } = new();
}

public enum AuthMode
{
    FigManaged,
    Keycloak
}

public class KeycloakAuthenticationSettings
{
    public string? Authority { get; set; }

    public string? Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;

    public string UsernameClaim { get; set; } = "preferred_username";

    public string FirstNameClaim { get; set; } = "given_name";

    public string LastNameClaim { get; set; } = "family_name";

    public string NameClaim { get; set; } = "name";

    public string RoleClaimPath { get; set; } = "realm_access.roles";

    public string? AdditionalRoleClaimPath { get; set; } = "resource_access.fig.roles";

    public string AllowedClassificationsClaim { get; set; } = "fig_allowed_classifications";

    public string ClientFilterClaim { get; set; } = "fig_client_filter";

    public string AdminRoleName { get; set; } = "Administrator";
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