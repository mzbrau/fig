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
        if (encryptedText is null)
            return null;

        if (!IsBase64String(encryptedText)) // this object has already been decrypted and is stored in the session.
            return encryptedText;
        
        return _cryptography.Decrypt(_apiSettings.Value.GetDecryptedSecret(),
                encryptedText,
                _apiSettings.Value.GetDecryptedPreviousSecret(),
                tryFallbackFirst);
    }
    
    private static bool IsBase64String(string base64)
    {
        Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
        return Convert.TryFromBase64String(base64, buffer , out _);
    }
}