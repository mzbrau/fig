using System.Diagnostics;
using System.Security.Cryptography;
using Fig.Api.Observability;
using Fig.Common.NetStandard.Cryptography;
using Microsoft.Extensions.Options;

namespace Fig.Api.Services;

public class EncryptionService : IEncryptionService
{
    private readonly ICryptography _cryptography;
    private readonly IOptionsMonitor<ApiSettings> _apiSettings;

    public EncryptionService(ICryptography cryptography, IOptionsMonitor<ApiSettings> apiSettings)
    {
        _cryptography = cryptography;
        _apiSettings = apiSettings;
    }

    public string? Encrypt(string? plainText)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        return plainText == null ? null : _cryptography.Encrypt(_apiSettings.CurrentValue.GetDecryptedSecret(), plainText);
    }

    public string? Decrypt(string? encryptedText, bool tryFallbackFirst = false, bool throwOnFailure = true)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        if (encryptedText is null)
            return null;

        try
        {
            return _cryptography.Decrypt(_apiSettings.CurrentValue.GetDecryptedSecret(),
                encryptedText,
                _apiSettings.CurrentValue.GetDecryptedPreviousSecret(),
                tryFallbackFirst);
        }
        catch (CryptographicException)
        {
            if (throwOnFailure)
                throw;
            
            return encryptedText; // TODO: Remove this, temporary fix for bug in 0.9.0
        }
        catch (FormatException)
        {
            if (throwOnFailure)
                throw;
            
            return encryptedText; // TODO: Remove this, temporary fix for bug in 0.9.0
        }
    }
}