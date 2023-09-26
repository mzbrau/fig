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

    public string? Decrypt(string? encryptedText, bool tryFallbackFirst = false)
    {
        return encryptedText == null
            ? null
            : _cryptography.Decrypt(_apiSettings.Value.GetDecryptedSecret(),
                encryptedText,
                _apiSettings.Value.GetDecryptedPreviousSecret(),
                tryFallbackFirst);
    }
}