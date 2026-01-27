namespace Fig.Api.Services;

/// <summary>
/// Provides in-memory caching for hash validation results to avoid expensive BCrypt operations.
/// </summary>
public interface IHashValidationCache
{
    /// <summary>
    /// Validates a client secret against a BCrypt hash, using the cache if available.
    /// </summary>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="clientSecret">The plain text client secret to validate.</param>
    /// <param name="bcryptHash">The BCrypt hash stored in the database.</param>
    /// <returns>True if the secret matches the hash, false otherwise.</returns>
    bool ValidateClientSecret(string clientName, string clientSecret, string bcryptHash);

    /// <summary>
    /// Validates a code hash, using the cache if available.
    /// </summary>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="settingName">The name of the setting.</param>
    /// <param name="code">The code to validate.</param>
    /// <param name="hash">The hash stored in the database.</param>
    /// <param name="computeHash">Function to compute the hash for validation when cache miss occurs.</param>
    /// <returns>True if the code matches the hash, false otherwise.</returns>
    bool ValidateCodeHash(string clientName, string settingName, string code, string hash, Func<string, string> computeHash);

    /// <summary>
    /// Invalidates all cached entries for a specific client.
    /// </summary>
    /// <param name="clientName">The name of the client to invalidate.</param>
    void InvalidateClient(string clientName);

    /// <summary>
    /// Cleans up expired cache entries.
    /// </summary>
    void CleanupExpiredEntries();
}
