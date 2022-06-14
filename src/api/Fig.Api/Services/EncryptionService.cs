using System.Net;
using System.Security;
using Fig.Common.Cryptography;
using Microsoft.Extensions.Options;

namespace Fig.Api.Services;

public class EncryptionService : IEncryptionService
{
    private readonly ICryptography _cryptography;
    private readonly SecureString _serverSecret;

    public EncryptionService(ICryptography cryptography, IOptions<ApiSettings> apiSettings)
    {
        _cryptography = cryptography;
        _serverSecret = new NetworkCredential(string.Empty, apiSettings.Value.Secret).SecurePassword;
    }

    public string? Encrypt(string? plainText)
    {
        return plainText == null ? null : _cryptography.Encrypt(_serverSecret, plainText);
    }

    public string? Decrypt(string? encryptedText)
    {
        return encryptedText == null ? null : _cryptography.Decrypt(_serverSecret, encryptedText);
    }
}