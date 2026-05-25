using System.Security.Cryptography;
using System.Text;
using Fig.Api.Services;
using Microsoft.Extensions.Options;

namespace Fig.Api.Validators;

public class CodeHasher : ICodeHasher
{
    private const string PreviousSecretCacheKeySuffix = ":previous-api-secret";

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

        return GetHash(code, secret);
    }

    public bool IsValid(string hash, string? code)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        var currentSecret = GetRequiredSecret();
        if (IsValidWithSecret(hash, code, currentSecret))
            return true;

        var previousSecret = _apiSettings.Value.GetDecryptedPreviousSecret();
        return IsDifferentPreviousSecret(previousSecret, currentSecret) &&
               IsValidWithSecret(hash, code, previousSecret!);
    }

    public bool IsValid(string clientName, string settingName, string hash, string? code)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        var currentSecret = GetRequiredSecret();

        if (_hashValidationCache.ValidateCodeHash(clientName,
                settingName,
                code,
                hash,
                value => GetHash(value, currentSecret)))
        {
            return true;
        }

        var previousSecret = _apiSettings.Value.GetDecryptedPreviousSecret();
        return IsDifferentPreviousSecret(previousSecret, currentSecret) &&
               _hashValidationCache.ValidateCodeHash($"{clientName}{PreviousSecretCacheKeySuffix}",
                   settingName,
                   code,
                   hash,
                   value => GetHash(value, previousSecret!));
    }

    private string GetRequiredSecret()
    {
        var secret = _apiSettings.Value.GetDecryptedSecret();
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("API secret is not configured.");

        return secret;
    }

    private static string GetHash(string code, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(hash);
    }

    private static bool IsValidWithSecret(string hash, string code, string secret)
    {
        try
        {
            var providedHashBytes = Convert.FromBase64String(hash);
            var computedHashBytes = Convert.FromBase64String(GetHash(code, secret));
            return CryptographicOperations.FixedTimeEquals(
                providedHashBytes,
                computedHashBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsDifferentPreviousSecret(string? previousSecret, string currentSecret)
    {
        return !string.IsNullOrWhiteSpace(previousSecret) &&
               !string.Equals(previousSecret, currentSecret, StringComparison.Ordinal);
    }
}