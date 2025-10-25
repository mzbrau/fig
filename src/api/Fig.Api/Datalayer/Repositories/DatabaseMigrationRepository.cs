using Fig.Api.DatabaseMigrations;
using Fig.Api.ExtensionMethods;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class DatabaseMigrationRepository : IDatabaseMigrationRepository
{
    private const string PendingStatus = "pending";
    private const string MigrationLockName = "fig_migration_gate";
    private const int LockTimeoutMs = 0; // NOWAIT equivalent
    private readonly ISession _session;
    private readonly ISessionFactory _sessionFactory;
    private readonly ILogger<DatabaseMigrationRepository> _logger;

    public DatabaseMigrationRepository(ISession session, ISessionFactory sessionFactory, ILogger<DatabaseMigrationRepository> logger)
    {
        _session = session;
        _sessionFactory = sessionFactory;
        _logger = logger;
    }

    public async Task<IList<DatabaseMigrationBusinessEntity>> GetExecutedMigrations()
    {
        try
        {
            var result = await _session.CreateQuery(
                    "FROM DatabaseMigrationBusinessEntity ORDER BY ExecutionNumber")
                .ListAsync<DatabaseMigrationBusinessEntity>();

            return result;
        }
        catch (Exception ex)
        {
            // Check if the table doesn't exist by examining exception types and error codes
            if (ex.IsTableNotExistsException())
            {
                _logger.LogDebug("Database migrations table does not exist yet, returning empty list");
                return new List<DatabaseMigrationBusinessEntity>();
            }

            throw;
        }
    }

    public async Task RecordMigrationExecution(DatabaseMigrationBusinessEntity migration)
    {
        await _session.SaveAsync(migration);
        await _session.FlushAsync();
    }

    public async Task ExecuteRawSql(string sql)
    {
        var query = _session.CreateSQLQuery(sql);
        await query.ExecuteUpdateAsync();
        await _session.FlushAsync();
    }

    public async Task<string> GetScriptForDatabase(IDatabaseMigration migration)
    {
        var connection = _session.Connection;
        var isSqlServer = IsSqlServer(connection.ConnectionString);

        var script = isSqlServer ? migration.SqlServerScript : migration.SqliteScript;

        _logger.LogDebug("Using {DatabaseType} script for migration {ExecutionNumber}",
            isSqlServer ? "SQL Server" : "SQLite", migration.ExecutionNumber);

        return await Task.FromResult(script);
    }

    public async Task<bool> TryBeginMigration(int executionNumber, string description)
    {
        _logger.LogDebug("Attempting to begin migration {ExecutionNumber}: {Description}", executionNumber,
            description);

        try
        {
            var connection = _session.Connection;
            var isSqlServer = IsSqlServer(connection.ConnectionString);
            _logger.LogTrace("Migration {ExecutionNumber}: using {DbType} strategy", executionNumber,
                isSqlServer ? "SQL Server" : "SQLite");

            return isSqlServer
                ? await TryBeginMigrationSqlServer(executionNumber, description)
                : await TryBeginMigrationSqlite(executionNumber, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while trying to begin migration {ExecutionNumber}", executionNumber);
            throw;
        }
    }

    public async Task CompleteMigration(int executionNumber, TimeSpan duration)
    {
        var entity = await _session.CreateQuery("FROM DatabaseMigrationBusinessEntity WHERE ExecutionNumber = :num")
            .SetParameter("num", executionNumber)
            .UniqueResultAsync<DatabaseMigrationBusinessEntity>();
        if (entity != null)
        {
            if (entity.Status != PendingStatus)
            {
                _logger.LogWarning(
                    "CompleteMigration called for migration {ExecutionNumber} but status was {Status} (expected 'pending')",
                    executionNumber, entity.Status);
                return;
            }

            entity.ExecutionDuration = duration;
            entity.ExecutedAt = DateTime.UtcNow;
            entity.Status = "complete";
            await _session.UpdateAsync(entity);
            await _session.FlushAsync();
            _logger.LogDebug("Marked migration {ExecutionNumber} as complete (duration {DurationMs}ms)",
                executionNumber, (long)duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogWarning("CompleteMigration called but no pending row found for migration {ExecutionNumber}",
                executionNumber);
        }
    }

    public async Task FailMigration(int executionNumber)
    {
        var entity = await _session.CreateQuery("FROM DatabaseMigrationBusinessEntity WHERE ExecutionNumber = :num")
            .SetParameter("num", executionNumber)
            .UniqueResultAsync<DatabaseMigrationBusinessEntity>();
        if (entity is { Status: PendingStatus })
        {
            await _session.DeleteAsync(entity);
            await _session.FlushAsync();
            _logger.LogWarning("Removed pending migration row for failed migration {ExecutionNumber}", executionNumber);
        }
        else if (entity == null)
        {
            _logger.LogWarning("FailMigration called but no row found for migration {ExecutionNumber}",
                executionNumber);
        }
        else
        {
            _logger.LogWarning(
                "FailMigration called for migration {ExecutionNumber} but status was {Status} (not 'pending')",
                executionNumber, entity.Status);
        }
    }

    private static bool IsSqlServer(string connectionString)
    {
        return !connectionString.Contains(".sqlite", StringComparison.OrdinalIgnoreCase) &&
               !connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) &&
               !connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> TryBeginMigrationSqlServer(int executionNumber, string description)
    {
        // Use a dedicated session and transaction to ensure the lock is released immediately
        // after the gate operation, regardless of any ambient transaction
        using var gateSession = _sessionFactory.OpenSession();
        using var gateTx = gateSession.BeginTransaction();
        
        var lockAcquired = false;
        try
        {
            // Acquire application lock with NOWAIT (0ms timeout)
            lockAcquired = await AcquireSqlServerAppLock(gateSession);
            
            if (!lockAcquired)
            {
                _logger.LogInformation(
                    "Migration {ExecutionNumber} lock contention detected (another instance). Skipping",
                    executionNumber);
                await gateTx.RollbackAsync();
                return false;
            }

            // Check if migration already exists
            if (await MigrationRowExists(gateSession, executionNumber))
            {
                _logger.LogInformation("Migration {ExecutionNumber} already present. Skipping", executionNumber);
                await gateTx.CommitAsync();
                return false;
            }

            // Insert pending migration row
            await InsertPendingMigrationRow(gateSession, executionNumber, description);
            await gateTx.CommitAsync();
            
            _logger.LogDebug("Migration {ExecutionNumber} pending row inserted", executionNumber);
            return true;
        }
        catch (Exception ex)
        {
            if (gateTx.IsActive)
                await gateTx.RollbackAsync();

            _logger.LogError(ex, "Failed to start migration {ExecutionNumber}", executionNumber);
            throw;
        }
        finally
        {
            // Always release the application lock
            if (lockAcquired)
            {
                try
                {
                    await ReleaseSqlServerAppLock(gateSession);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to release app lock for migration {ExecutionNumber}", executionNumber);
                }
            }
        }
    }

    private async Task<bool> TryBeginMigrationSqlite(int executionNumber, string description)
    {
        if (await MigrationRowExists(_session, executionNumber))
        {
            _logger.LogInformation("Migration {ExecutionNumber} already present in SQLite. Skipping", executionNumber);
            return false;
        }

        await InsertPendingMigrationRow(_session, executionNumber, description);
        _logger.LogDebug("Migration {ExecutionNumber} pending row inserted (SQLite)", executionNumber);
        return true;
    }

    private async Task<bool> AcquireSqlServerAppLock(ISession session)
    {
        _logger.LogTrace("Acquiring application lock '{LockName}'", MigrationLockName);
        
        var lockQuery = session.CreateSQLQuery(
            @"DECLARE @result INT;
              EXEC @result = sp_getapplock 
                  @Resource = :lockName,
                  @LockMode = 'Exclusive',
                  @LockOwner = 'Transaction',
                  @LockTimeout = :timeout;
              SELECT @result;");
        
        lockQuery.SetParameter("lockName", MigrationLockName);
        lockQuery.SetParameter("timeout", LockTimeoutMs);
        
        var result = await lockQuery.UniqueResultAsync<int>();
        
        // sp_getapplock return values:
        // >= 0: Lock granted
        // -1: Timeout
        // -2: Cancelled
        // -3: Deadlock victim
        // -999: Parameter validation or other error
        
        if (result >= 0)
        {
            _logger.LogTrace("Application lock '{LockName}' acquired (result: {Result})", MigrationLockName, result);
            return true;
        }
        
        _logger.LogDebug("Failed to acquire application lock '{LockName}' (result: {Result})", MigrationLockName, result);
        return false;
    }

    private async Task ReleaseSqlServerAppLock(ISession session)
    {
        _logger.LogTrace("Releasing application lock '{LockName}'", MigrationLockName);
        
        var releaseQuery = session.CreateSQLQuery(
            @"DECLARE @result INT;
              EXEC @result = sp_releaseapplock 
                  @Resource = :lockName,
                  @LockOwner = 'Transaction';
              SELECT @result;");
        
        releaseQuery.SetParameter("lockName", MigrationLockName);
        
        var result = await releaseQuery.UniqueResultAsync<int>();
        
        _logger.LogTrace("Application lock '{LockName}' released (result: {Result})", MigrationLockName, result);
    }

    private async Task<bool> MigrationRowExists(ISession session, int executionNumber)
    {
        var existing = await session.CreateQuery("FROM DatabaseMigrationBusinessEntity WHERE ExecutionNumber = :num")
            .SetParameter("num", executionNumber)
            .ListAsync<DatabaseMigrationBusinessEntity>();
        return existing.Any();
    }

    private async Task InsertPendingMigrationRow(ISession session, int executionNumber, string description)
    {
        var entity = new DatabaseMigrationBusinessEntity
        {
            ExecutionNumber = executionNumber,
            Description = description,
            ExecutedAt = DateTime.UtcNow,
            ExecutionDuration = TimeSpan.Zero,
            Status = PendingStatus
        };
        await session.SaveAsync(entity);
        await session.FlushAsync();
    }
}