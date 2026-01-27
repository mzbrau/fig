using BenchmarkDotNet.Attributes;
using Fig.Api;
using Fig.Api.Services;
using Fig.Api.Validators;
using Microsoft.Extensions.Options;
using Moq;

namespace Fig.Benchmarks.Test;

public class CodeHashBenchmarks
{
    private const string Code = """
                                public class NewCodeHasher : INewCodeHasher
                                {
                                    private readonly IOptions<ApiSettings> _apiSettings;

                                    public NewCodeHasher(IOptions<ApiSettings> apiSettings)
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

                                        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes($"{secret} {code}"));
                                        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{secret} {code}"));
                                        return Convert.ToBase64String(hash);
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

                                        var computedHash = GetHash(code);

                                        // Constant-time comparison
                                        return CryptographicOperations.FixedTimeEquals(
                                            Convert.FromBase64String(hash),
                                            Convert.FromBase64String(computedHash)
                                        );
                                    }

                                """;
    
    [Benchmark]
    public void HashAndVerifyHashWithBcrypt()
    {
        var options = Options.Create(new ApiSettings()
        {
            Secret = "20d00903cbcf4190bc6d6d4ad9c5a6e3bbda143979894f5c9d4d67a5b7b1497b3641478e917246e39e86232d8fcd317",
            DbConnectionString = string.Empty
        });
        
        var hasher = new LegacyCodeHasher(options);
        var hash = hasher.GetHash(Code);
        var isValid = hasher.IsValid(hash, Code);

        if (!isValid)
            throw new Exception("Hash is not valid.");
    }
    
    [Benchmark]
    public void HashAndVerifyHashWithHmac()
    {
        var options = Options.Create(new ApiSettings()
        {
            Secret = "20d00903cbcf4190bc6d6d4ad9c5a6e3bbda143979894f5c9d4d67a5b7b1497b3641478e917246e39e86232d8fcd317",
            DbConnectionString = string.Empty
        });
        var mockHashValidationCache = new Mock<IHashValidationCache>();

        var hasher = new CodeHasher(options, mockHashValidationCache.Object);
        var hash = hasher.GetHash(Code);
        var isValid = hasher.IsValid(hash, Code);

        if (!isValid)
            throw new Exception("Hash is not valid.");
    }
}