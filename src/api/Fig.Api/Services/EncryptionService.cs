using System.Security;
using Fig.Common.Cryptography;
using Microsoft.Extensions.Options;

namespace Fig.Api.Services;

public class EncryptionService : IEncryptionService
{
    private readonly ICryptography _cryptography;
    private readonly List<SecureString>? _previousSecrets;
    private readonly SecureString _serverSecret;

    public EncryptionService(ICryptography cryptography, IOptions<ApiSettings> apiSettings)
    {
        _cryptography = cryptography;
        _serverSecret = apiSettings.Value.Secret.ToSecureString();
        _previousSecrets = apiSettings.Value.PreviousSecrets?.Where(a => !string.IsNullOrEmpty(a))
            .Select(a => a.ToSecureString()).ToList();
    }

    public string? Encrypt(string? plainText)
    {
        return plainText == null ? null : _cryptography.Encrypt(_serverSecret, plainText);
    }

    public string? Decrypt(string? encryptedText)
    {
        return encryptedText == null ? null : _cryptography.Decrypt(_serverSecret, encryptedText, _previousSecrets);
    }
}