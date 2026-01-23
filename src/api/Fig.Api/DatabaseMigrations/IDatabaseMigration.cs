namespace Fig.Api.DatabaseMigrations;

/// <summary>
/// Fig uses NHibernate to automatically create the database schema based on the model classes.
/// However, it does not update existing rows if they require changes. These migrations can be used to make these changes instead.
/// </summary>
public interface IDatabaseMigration
{
    /// <summary>
    /// The execution order of this migration. Must be sequential starting from 1.
    /// </summary>
    int ExecutionNumber { get; }
    
    /// <summary>
    /// A human-readable description of what this migration does.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// The SQL script to execute for SQL Server databases.
    /// </summary>
    string SqlServerScript { get; }
    
    /// <summary>
    /// The SQL script to execute for SQLite databases.
    /// </summary>
    string SqliteScript { get; }
    
    /// <summary>
    /// Optional: Execute code-based migration logic instead of or in addition to SQL scripts.
    /// If this method is implemented and returns a non-null task, it will be executed.
    /// If both SQL scripts and code execution are provided, code execution runs first.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>Task to await, or null if no code execution is needed</returns>
    Task? ExecuteCode(IServiceProvider serviceProvider) => null;
}
