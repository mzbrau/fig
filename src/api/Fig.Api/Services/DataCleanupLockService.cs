using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Services;

/// <summary>
/// Database-backed locking service to ensure only one API instance performs data cleanup at a time.
/// Uses the api_status table to manage distributed locks.
/// </summary>
public class DataCleanupLockService : IDataCleanupLockService
{
    private const string LockName = "DataCleanupLock";
    private const int LockTimeoutMinutes = 30; // Maximum time a lock can be held
    private readonly ISession _session;
    private readonly ILogger<DataCleanupLockService> _logger;
    private Guid? _lockId;

    public DataCleanupLockService(ISession session, ILogger<DataCleanupLockService> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<bool> TryAcquireLockAsync()
    {
        try
        {
            using var transaction = _session.BeginTransaction();
            
            // Try to acquire lock with NOWAIT to avoid blocking
            var criteria = _session.CreateCriteria<ApiStatusBusinessEntity>();
            criteria.Add(Restrictions.Eq(nameof(ApiStatusBusinessEntity.Hostname), LockName));
            
            try
            {
                criteria.SetLockMode(LockMode.Upgrade);
                var existingLock = await criteria.UniqueResultAsync<ApiStatusBusinessEntity>();
                
                if (existingLock != null)
                {
                    // Check if lock has expired
                    if (existingLock.LastSeen > DateTime.UtcNow.AddMinutes(-LockTimeoutMinutes))
                    {
                        _logger.LogDebug("Data cleanup lock is already held by another instance");
                        await transaction.RollbackAsync();
                        return false;
                    }
                    
                    // Lock has expired, take ownership
                    _logger.LogInformation("Previous data cleanup lock expired, taking ownership");
                    existingLock.LastSeen = DateTime.UtcNow;
                    await _session.UpdateAsync(existingLock);
                    _lockId = existingLock.Id;
                }
                else
                {
                    // No existing lock, create one
                    var newLock = new ApiStatusBusinessEntity
                    {
                        Hostname = LockName,
                        IpAddress = "DataCleanup",
                        StartTimeUtc = DateTime.UtcNow,
                        LastSeen = DateTime.UtcNow,
                        IsActive = true,
                        Version = "lock",
                        RunningUser = "system",
                        RuntimeId = Guid.NewGuid(),
                        SecretHash = "lock"
                    };
                    
                    await _session.SaveAsync(newLock);
                    _lockId = newLock.Id;
                    _logger.LogDebug("Acquired data cleanup lock");
                }
                
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex) when (IsLockException(ex))
            {
                _logger.LogDebug(ex, "Could not acquire lock immediately, another instance may be performing cleanup");
                await transaction.RollbackAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to acquire data cleanup lock");
            return false;
        }
    }

    public async Task ReleaseLockAsync()
    {
        if (_lockId == null)
        {
            _logger.LogWarning("Attempted to release lock that was not acquired");
            return;
        }

        try
        {
            var lockEntity = await _session.GetAsync<ApiStatusBusinessEntity>(_lockId);
            if (lockEntity != null)
            {
                await _session.DeleteAsync(lockEntity);
                await _session.FlushAsync();
                _logger.LogDebug("Released data cleanup lock");
            }
            
            _lockId = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing data cleanup lock");
        }
    }
    
    private bool IsLockException(Exception ex)
    {
        // Check for SQL Server lock timeout or SQLite busy exceptions
        return ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("busy", StringComparison.OrdinalIgnoreCase);
    }
}
