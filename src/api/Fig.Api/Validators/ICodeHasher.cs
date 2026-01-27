namespace Fig.Api.Validators;

public interface ICodeHasher
{
    string GetHash(string code);

    bool IsValid(string hash, string? code);

    /// <summary>
    /// Validates a code hash using the in-memory cache for improved performance.
    /// </summary>
    /// <param name="clientName">The client name for cache key.</param>
    /// <param name="settingName">The setting name for cache key.</param>
    /// <param name="hash">The stored hash to validate against.</param>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValid(string clientName, string settingName, string hash, string? code);
}