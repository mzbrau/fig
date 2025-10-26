using System.Collections.Concurrent;

namespace Fig.Api.Services;

public class ClientRegistrationLockService : IClientRegistrationLockService
{
    private readonly ConcurrentDictionary<string, LockEntry> _clientLocks = new();
    private readonly TimeSpan _lockExpirationTime = TimeSpan.FromMinutes(30);

    public async Task<IDisposable> AcquireLockAsync(string clientName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("Client name cannot be null or whitespace.", nameof(clientName));
        
        var lockEntry = _clientLocks.GetOrAdd(clientName, _ => new LockEntry());
        lockEntry.UpdateLastAccessTime();
        
        await lockEntry.Semaphore.WaitAsync(cancellationToken);
        lockEntry.IncrementActiveCount();
        
        return new LockReleaser(lockEntry);
    }

    public void CleanupUnusedLocks()
    {
        var now = DateTime.UtcNow;
        
        // Take a snapshot to avoid modification during enumeration
        var lockSnapshot = _clientLocks.ToList();
        
        foreach (var kvp in lockSnapshot)
        {
            var key = kvp.Key;
            var entry = kvp.Value;
            
            // Try to acquire the semaphore non-blocking to ensure it's not in use
            if (!entry.Semaphore.Wait(0))
            {
                // Someone is using it, skip cleanup
                continue;
            }

            try
            {
                // Revalidate under the semaphore - recompute now for this specific entry
                var currentTime = DateTime.UtcNow;
                
                // Only remove if:
                // 1. No active users (lock not currently held)
                // 2. Last access was beyond expiration time
                if (entry.ActiveCount == 0 && 
                    (currentTime - entry.LastAccessTime) > _lockExpirationTime)
                {
                    // Verify the dictionary still maps this exact instance before removal
                    if (_clientLocks.TryGetValue(key, out var currentEntry) && 
                        ReferenceEquals(currentEntry, entry))
                    {
                        if (_clientLocks.TryRemove(key, out _))
                        {
                            // Successfully removed, dispose the semaphore
                            // Note: We don't release because we're disposing
                            entry.Semaphore.Dispose();
                            continue; // Skip the Release() in finally
                        }
                    }
                }
                
                // If we couldn't remove or validation failed, release the semaphore
                entry.Semaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                // Semaphore was already disposed by another thread, this is fine
                // Just continue to the next entry
            }
            catch
            {
                // Ensure we always release if we acquired and didn't dispose
                try { entry.Semaphore.Release(); } catch (ObjectDisposedException) { /* Already disposed */ }
                throw;
            }
        }
    }

    private class LockEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public DateTime LastAccessTime { get; private set; } = DateTime.UtcNow;
        private int _activeCount;
        
        public int ActiveCount => _activeCount;

        public void UpdateLastAccessTime()
        {
            LastAccessTime = DateTime.UtcNow;
        }

        public void IncrementActiveCount()
        {
            Interlocked.Increment(ref _activeCount);
        }

        public void DecrementActiveCount()
        {
            Interlocked.Decrement(ref _activeCount);
        }
    }

    private class LockReleaser : IDisposable
    {
        private readonly LockEntry _lockEntry;
        private bool _disposed;

        public LockReleaser(LockEntry lockEntry)
        {
            _lockEntry = lockEntry;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _lockEntry.DecrementActiveCount();
                _lockEntry.UpdateLastAccessTime(); // Update timestamp on release
                _lockEntry.Semaphore.Release();
                _disposed = true;
            }
        }
    }
}
