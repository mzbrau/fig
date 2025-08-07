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
    /// Attempts to mark a migration as pending by inserting a row with status 'pending'.
    /// Uses a table lock to ensure only one instance can insert for a given execution number at a time.
    /// Returns true if this instance should execute the migration, false if another already is/has.
    /// </summary>
    Task<bool> TryBeginMigration(int executionNumber, string description);

    /// <summary>
    /// Marks a pending migration as complete (updates status and timing info).
    /// </summary>
    Task CompleteMigration(int executionNumber, TimeSpan duration);

    /// <summary>
    /// Removes the row for a failed migration (so it can be retried on next startup).
    /// </summary>
    Task FailMigration(int executionNumber);
}
