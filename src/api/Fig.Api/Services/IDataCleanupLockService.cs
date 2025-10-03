namespace Fig.Api.Services;

/// <summary>
/// Service for acquiring and releasing locks to coordinate data cleanup operations across multiple API instances.
/// </summary>
public interface IDataCleanupLockService
{
    /// <summary>
    /// Attempts to acquire a lock for data cleanup operations.
    /// </summary>
    /// <returns>True if the lock was acquired, false if another instance already holds the lock.</returns>
    Task<bool> TryAcquireLockAsync();
    
    /// <summary>
    /// Releases the lock for data cleanup operations.
    /// </summary>
    Task ReleaseLockAsync();
}
