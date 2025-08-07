using Fig.Api.DatabaseMigrations;
using Fig.Api.ExtensionMethods;
using Fig.Datalayer.BusinessEntities;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class DatabaseMigrationRepository : IDatabaseMigrationRepository
{
    private const string PendingStatus = "pending";
    private readonly ISession _session;
    private readonly ILogger<DatabaseMigrationRepository> _logger;

    public DatabaseMigrationRepository(ISession session, ILogger<DatabaseMigrationRepository> logger)
    {
        _session = session;
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
        using var tx = _session.BeginTransaction();
        try
        {
            await AcquireSqlServerTableLock();

            if (await MigrationRowExists(executionNumber))
            {
                await tx.CommitAsync();
                _logger.LogInformation("Migration {ExecutionNumber} already present. Skipping", executionNumber);
                return false;
            }

            await InsertPendingMigrationRow(executionNumber, description);
            await tx.CommitAsync();
            _logger.LogDebug("Migration {ExecutionNumber} pending row inserted", executionNumber);
            return true;
        }
        catch (Exception ex)
        {
            if (tx.IsActive)
                await tx.RollbackAsync();

            if (ex.IsLockContention())
            {
                _logger.LogInformation(
                    "Migration {ExecutionNumber} lock contention detected (another instance). Skipping",
                    executionNumber);
                return false;
            }

            _logger.LogError(ex, "Failed to start migration {ExecutionNumber}", executionNumber);
            throw;
        }
    }

    private async Task<bool> TryBeginMigrationSqlite(int executionNumber, string description)
    {
        if (await MigrationRowExists(executionNumber))
        {
            _logger.LogInformation("Migration {ExecutionNumber} already present in SQLite. Skipping", executionNumber);
            return false;
        }

        await InsertPendingMigrationRow(executionNumber, description);
        _logger.LogDebug("Migration {ExecutionNumber} pending row inserted (SQLite)", executionNumber);
        return true;
    }

    private async Task AcquireSqlServerTableLock()
    {
        _logger.LogTrace("Acquiring table lock on database_migrations");
        var lockQuery =
            _session.CreateSQLQuery("SELECT TOP(1) 1 FROM database_migrations WITH (TABLOCKX, HOLDLOCK, NOWAIT)");
        await lockQuery.UniqueResultAsync<int>();
        _logger.LogTrace("Table lock acquired");
    }

    private async Task<bool> MigrationRowExists(int executionNumber)
    {
        var existing = await _session.CreateQuery("FROM DatabaseMigrationBusinessEntity WHERE ExecutionNumber = :num")
            .SetParameter("num", executionNumber)
            .ListAsync<DatabaseMigrationBusinessEntity>();
        return existing.Any();
    }

    private async Task InsertPendingMigrationRow(int executionNumber, string description)
    {
        var entity = new DatabaseMigrationBusinessEntity
        {
            ExecutionNumber = executionNumber,
            Description = description,
            ExecutedAt = DateTime.UtcNow,
            ExecutionDuration = TimeSpan.Zero,
            Status = PendingStatus
        };
        await _session.SaveAsync(entity);
        await _session.FlushAsync();
    }
}