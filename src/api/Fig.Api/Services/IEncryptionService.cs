namespace Fig.Api.Services;

public interface IEncryptionService
{
    string? Encrypt(string? plainText);

    string? Decrypt(string? encryptedText, bool tryFallbackFirst = false, bool throwOnFailure = true);

    /// <summary>
    /// Decrypts using the configured API secret and previous API secret, accepting only plaintext that passes
    /// <paramref name="isValid"/>. The validator may be called more than once and should not rely on side effects.
    /// </summary>
    string? DecryptWithValidation(
        string? encryptedText,
        Func<string, bool> isValid,
        bool tryFallbackFirst = false,
        ValidatedDecryptionMode mode = ValidatedDecryptionMode.Strict);
    string? DecryptWithCustomKey(string? encryptedText, string customKey);
    string? DecryptForImport(string? encryptedText, string? customDecryptionKey = null);
}
