namespace Fig.Api.Services;

public interface IEncryptionService
{
    string? Encrypt(string? plainText);

    string? Decrypt(string? encryptedText, bool tryFallbackFirst = false, bool throwOnFailure = true);

    string? DecryptWithValidation(
        string? encryptedText,
        Func<string, bool> isValid,
        bool tryFallbackFirst = false,
        ValidatedDecryptionMode mode = ValidatedDecryptionMode.Strict);
    string? DecryptWithCustomKey(string? encryptedText, string customKey);
    string? DecryptForImport(string? encryptedText, string? customDecryptionKey = null);
}
