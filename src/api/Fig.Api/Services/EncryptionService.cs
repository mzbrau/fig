using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Fig.Common.NetStandard.Cryptography;
using Microsoft.Extensions.Options;

namespace Fig.Api.Services;

public class EncryptionService : IEncryptionService
{
    private readonly ICryptography _cryptography;
    private readonly IOptions<ApiSettings> _apiSettings;

    public EncryptionService(ICryptography cryptography, IOptions<ApiSettings> apiSettings)
    {
        _cryptography = cryptography;
        _apiSettings = apiSettings;
    }

    public string? Encrypt(string? plainText)
    {
        return plainText == null ? null : _cryptography.Encrypt(_apiSettings.Value.GetDecryptedSecret(), plainText);
    }

    public string? Decrypt(string? encryptedText, bool tryFallbackFirst = false, bool throwOnFailure = true)
    {
        if (encryptedText is null)
            return null;

        try
        {
            return _cryptography.Decrypt(_apiSettings.Value.GetDecryptedSecret(),
                encryptedText,
                _apiSettings.Value.GetDecryptedPreviousSecret(),
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