using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Fig.Api.Services;

/// <summary>
/// In-memory cache for hash validation results to avoid expensive BCrypt operations.
/// Uses SHA256 fingerprints of secrets to verify cache validity.
/// </summary>
public class HashValidationCache : IHashValidationCache
{
    private const string ClientSecretPrefix = "client-secret:";
    private const string CodeHashPrefix = "code-hash:";
    
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _cacheExpiry;
    private readonly bool _cacheEnabled;

    public HashValidationCache(IOptions<ApiSettings> apiSettings)
    {
        var expiryMinutes = apiSettings.Value.HashCacheExpiryMinutes;
        _cacheEnabled = expiryMinutes > 0;
        _cacheExpiry = TimeSpan.FromMinutes(expiryMinutes);
    }

    public bool ValidateClientSecret(string clientName, string clientSecret, string bcryptHash)
    {
        if (!_cacheEnabled)
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, bcryptHash);
        }

        var cacheKey = GetClientSecretCacheKey(clientName);
        var secretFingerprint = ComputeFingerprint(clientSecret);

        if (_cache.TryGetValue(cacheKey, out var entry) && !entry.IsExpired(_cacheExpiry))
        {
            // Verify the cached BCrypt hash matches the database hash
            if (entry.StoredHash == bcryptHash && 
                CryptographicOperations.FixedTimeEquals(entry.SecretFingerprint, secretFingerprint))
            {
                return entry.IsValid;
            }
        }

        // Cache miss or hash changed - perform BCrypt verification
        var isValid = BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, bcryptHash);
        
        _cache[cacheKey] = new CacheEntry
        {
            StoredHash = bcryptHash,
            SecretFingerprint = secretFingerprint,
            IsValid = isValid,
            CreatedAtUtc = DateTime.UtcNow
        };

        return isValid;
    }

    public bool ValidateCodeHash(string clientName, string settingName, string code, string hash, Func<string, string> computeHash)
    {
        if (!_cacheEnabled)
        {
            return ValidateCodeHashDirectly(code, hash, computeHash);
        }

        var cacheKey = GetCodeHashCacheKey(clientName, settingName);
        var codeFingerprint = ComputeFingerprint(code);

        if (_cache.TryGetValue(cacheKey, out var entry) && !entry.IsExpired(_cacheExpiry))
        {
            // Verify the cached hash matches the database hash
            if (entry.StoredHash == hash && 
                CryptographicOperations.FixedTimeEquals(entry.SecretFingerprint, codeFingerprint))
            {
                return entry.IsValid;
            }
        }

        // Cache miss or hash changed - perform hash validation
        var isValid = ValidateCodeHashDirectly(code, hash, computeHash);
        
        _cache[cacheKey] = new CacheEntry
        {
            StoredHash = hash,
            SecretFingerprint = codeFingerprint,
            IsValid = isValid,
            CreatedAtUtc = DateTime.UtcNow
        };

        return isValid;
    }

    public void InvalidateClient(string clientName)
    {
        // Remove all entries for this client (both secret and code hash entries)
        // Use prefixes to identify client-related entries
        var clientSecretKey = GetClientSecretCacheKey(clientName);
        var codeHashKeyPrefix = $"{CodeHashPrefix}{clientName}:";
        
        // Collect keys to remove (avoid enumeration issues during removal)
        var keysToRemove = new List<string>();
        foreach (var key in _cache.Keys)
        {
            if (key == clientSecretKey || key.StartsWith(codeHashKeyPrefix))
            {
                keysToRemove.Add(key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void CleanupExpiredEntries()
    {
        if (!_cacheEnabled)
            return;

        // Collect keys to remove in a single pass
        var keysToRemove = new List<string>();
        foreach (var kvp in _cache)
        {
            if (kvp.Value.IsExpired(_cacheExpiry))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    private static byte[] ComputeFingerprint(string secret)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    private static string GetClientSecretCacheKey(string clientName)
    {
        return $"{ClientSecretPrefix}{clientName}";
    }

    private static string GetCodeHashCacheKey(string clientName, string settingName)
    {
        return $"{CodeHashPrefix}{clientName}:{settingName}";
    }

    private static bool ValidateCodeHashDirectly(string code, string hash, Func<string, string> computeHash)
    {
        try
        {
            var computedHash = computeHash(code);
            var providedHashBytes = Convert.FromBase64String(hash);
            var computedHashBytes = Convert.FromBase64String(computedHash);
            return CryptographicOperations.FixedTimeEquals(providedHashBytes, computedHashBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private class CacheEntry
    {
        public required string StoredHash { get; init; }
        public required byte[] SecretFingerprint { get; init; }
        public required bool IsValid { get; init; }
        public required DateTime CreatedAtUtc { get; init; }

        public bool IsExpired(TimeSpan expiry) => DateTime.UtcNow - CreatedAtUtc > expiry;
    }
}
