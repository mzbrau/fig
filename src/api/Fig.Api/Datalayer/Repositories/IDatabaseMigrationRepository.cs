using Fig.Api.DatabaseMigrations;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IDatabaseMigrationRepository
{
    /// <summary>
    /// Gets all executed migrations ordered by execution number.
    /// </summary>
    Task<IList<DatabaseMigrationBusinessEntity>> GetExecutedMigrations();
    
    /// <summary>
    /// Records a successful migration execution.
    /// </summary>
    /// <param name="migration">The migration record to save</param>
    Task RecordMigrationExecution(DatabaseMigrationBusinessEntity migration);
    
    /// <summary>
    /// Executes raw SQL within the current session.
    /// </summary>
    /// <param name="sql">The SQL to execute</param>
    Task ExecuteRawSql(string sql);
    
    /// <summary>
    /// Gets the appropriate script for the current database type.
    /// </summary>
    /// <param name="migration">The migration to get the script for</param>
    /// <returns>The SQL script for the current database type</returns>
    Task<string> GetScriptForDatabase(IDatabaseMigration migration);
    
    /// <summary>
    /// Attempts to acquire a distributed lock for migrations.
    /// Returns true if lock was acquired, false if another process holds it.
    /// </summary>
    /// <param name="timeoutSeconds">How long to wait for the lock</param>
    Task<bool> TryAcquireMigrationLock(int timeoutSeconds = 30);
    
    /// <summary>
    /// Releases the migration lock.
    /// </summary>
    Task ReleaseMigrationLock();
}
