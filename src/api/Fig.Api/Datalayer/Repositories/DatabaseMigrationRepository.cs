using Fig.Api.DatabaseMigrations;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class DatabaseMigrationRepository : IDatabaseMigrationRepository
{
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
            // If the table doesn't exist yet, return empty list
            if (ex.Message.Contains("Invalid object name 'database_migrations'") || 
                ex.Message.Contains("no such table: database_migrations"))
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

    public async Task<bool> TryAcquireMigrationLock(int timeoutSeconds = 30)
    {
        try
        {
            var connection = _session.Connection;
            
            if (IsSqlServer(connection.ConnectionString))
            {
                return await TryAcquireSqlServerLock(timeoutSeconds);
            }

            // For SQLite (typically development), skip locking for simplicity
            // In production, SQLite is rarely used with multiple instances
            _logger.LogDebug("Skipping migration lock for SQLite database");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire migration lock");
            return false;
        }
    }

    public async Task ReleaseMigrationLock()
    {
        try
        {
            var connection = _session.Connection;
            
            if (IsSqlServer(connection.ConnectionString))
            {
                await ReleaseSqlServerLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release migration lock");
        }
    }

    private async Task<bool> TryAcquireSqlServerLock(int timeoutSeconds)
    {
        var query = _session.CreateSQLQuery(@"
            DECLARE @result INT;
            EXEC @result = sp_getapplock 
                @Resource = 'FigDatabaseMigration', 
                @LockMode = 'Exclusive', 
                @LockOwner = 'Session',
                @LockTimeout = :timeout;
            SELECT @result AS Result");
        
        query.SetParameter("timeout", timeoutSeconds * 1000); // Convert to milliseconds
        
        var result = await query.UniqueResultAsync<int>();
        
        // sp_getapplock returns 0 or 1 for success
        return result >= 0;
    }

    private async Task ReleaseSqlServerLock()
    {
        var query = _session.CreateSQLQuery(@"
            EXEC sp_releaseapplock 
                @Resource = 'FigDatabaseMigration', 
                @LockOwner = 'Session'");
        
        await query.ExecuteUpdateAsync();
    }

    private static bool IsSqlServer(string connectionString)
    {
        return !connectionString.Contains(".sqlite", StringComparison.OrdinalIgnoreCase) &&
               !connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) &&
               !connectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase);
    }
}
