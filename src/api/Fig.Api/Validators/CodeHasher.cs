using System.Security.Cryptography;
using System.Text;
using Fig.Api.Services;
using Microsoft.Extensions.Options;

namespace Fig.Api.Validators;

public class CodeHasher : ICodeHasher
{
    private readonly IOptions<ApiSettings> _apiSettings;
    private readonly IHashValidationCache _hashValidationCache;

    public CodeHasher(IOptions<ApiSettings> apiSettings, IHashValidationCache hashValidationCache)
    {
        _apiSettings = apiSettings;
        _hashValidationCache = hashValidationCache;
    }

    public string GetHash(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));

        var secret = _apiSettings.Value.GetDecryptedSecret();
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("API secret is not configured.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(hash);
    }

    public bool IsValid(string hash, string? code)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        var secret = _apiSettings.Value.GetDecryptedSecret();
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("API secret is not configured.");

        // Constant-time comparison
        try
        {
            var providedHashBytes = Convert.FromBase64String(hash);
            var computedHashBytes = Convert.FromBase64String(GetHash(code));
            return CryptographicOperations.FixedTimeEquals(
                providedHashBytes,
                computedHashBytes);
        }
        catch (FormatException)
        {
            // Invalid Base64 encoding should be treated as an invalid hash
            return false;
        }
    }

    public bool IsValid(string clientName, string settingName, string hash, string? code)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        var secret = _apiSettings.Value.GetDecryptedSecret();
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("API secret is not configured.");

        return _hashValidationCache.ValidateCodeHash(clientName, settingName, code, hash, GetHash);
    }
}