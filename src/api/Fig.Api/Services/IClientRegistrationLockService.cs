namespace Fig.Api.Services;

public interface IClientRegistrationLockService
{
    Task<IDisposable> AcquireLockAsync(string clientName, CancellationToken cancellationToken = default);
    
    void CleanupUnusedLocks();
}
