using Microsoft.Extensions.Options;

namespace Fig.Api.Validators;

public class CodeHasher : ICodeHasher
{
    private readonly IOptions<ApiSettings> _apiSettings;

    public CodeHasher(IOptions<ApiSettings> apiSettings)
    {
        _apiSettings = apiSettings;
    }

    public string GetHash(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));

        var secret = _apiSettings.Value.Secret;
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("API secret is not configured.");

        return BCrypt.Net.BCrypt.EnhancedHashPassword($"{code}{secret}");
    }

    public bool IsValid(string hash, string? code)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        var secret = _apiSettings.Value.Secret;
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("API secret is not configured.");

        return BCrypt.Net.BCrypt.EnhancedVerify($"{code}{secret}", hash);
    }
}