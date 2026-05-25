using System.Security.Cryptography;
using Fig.Api.Exceptions;
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
        return plainText == null ? null : _cryptography.Encrypt(_apiSettings.CurrentValue.GetDecryptedSecret(), plainText);
    }

    public string? Decrypt(string? encryptedText, bool tryFallbackFirst = false, bool throwOnFailure = true)
    {
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

    public string? DecryptWithValidation(string? encryptedText, Func<string, bool> isValid, bool tryFallbackFirst = false)
    {
        ArgumentNullException.ThrowIfNull(isValid);

        if (encryptedText is null)
            return null;

        var validValues = new List<string>();
        Exception? firstFailure = null;

        foreach (var key in GetDecryptionKeys(tryFallbackFirst))
        {
            try
            {
                var decrypted = _cryptography.Decrypt(key, encryptedText);
                if (isValid(decrypted))
                    validValues.Add(decrypted);
                else
                    firstFailure ??= new CryptographicException("Decrypted value did not pass validation.");
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                firstFailure ??= ex;
            }
        }

        return validValues.Count switch
        {
            1 => validValues[0],
            > 1 => throw new CryptographicException("Multiple configured API secrets produced valid decrypted values."),
            _ => throw new CryptographicException("Unable to decrypt value with the configured API secret or previous API secret.", firstFailure)
        };
    }

    public string? DecryptWithCustomKey(string? encryptedText, string customKey)
    {
        if (encryptedText is null)
            return null;

        try
        {
            return _cryptography.Decrypt(customKey, encryptedText);
        }
        catch (Exception ex)
        {
            throw new InvalidPasswordException("Decryption failed with the provided custom key", ex);
        }
    }

    public string? DecryptForImport(string? encryptedText, string? customDecryptionKey = null)
    {
        return string.IsNullOrWhiteSpace(customDecryptionKey)
            ? Decrypt(encryptedText)
            : DecryptWithCustomKey(encryptedText, customDecryptionKey);
    }

    private IEnumerable<string> GetDecryptionKeys(bool tryFallbackFirst)
    {
        var currentSecret = _apiSettings.CurrentValue.GetDecryptedSecret();
        var previousSecret = _apiSettings.CurrentValue.GetDecryptedPreviousSecret();
        var keys = new List<string>(2);

        if (tryFallbackFirst && !string.IsNullOrWhiteSpace(previousSecret))
            keys.Add(previousSecret);

        keys.Add(currentSecret);

        if (!tryFallbackFirst && !string.IsNullOrWhiteSpace(previousSecret))
            keys.Add(previousSecret);

        return keys.Distinct(StringComparer.Ordinal);
    }
}
