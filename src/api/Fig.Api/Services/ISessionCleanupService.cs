namespace Fig.Api.Services;

/// <summary>
/// Service for cleaning up expired client run sessions.
/// </summary>
public interface ISessionCleanupService
{
    /// <summary>
    /// Removes expired run sessions from all clients.
    /// </summary>
    /// <returns>The total number of sessions removed.</returns>
    Task<int> RemoveExpiredSessionsAsync();
}
