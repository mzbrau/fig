using Fig.Api.DatabaseMigrations;

namespace Fig.Api.DatabaseMigrations.Migrations;

public class Migration_001_IncreaseValidationRegexLength : IDatabaseMigration
{
    public int ExecutionNumber => 1;
    
    public string Description => "Increase validation_regex column length to NVARCHAR(MAX)";
    
    public string SqlServerScript => GetSqlServerScript();
    
    public string SqliteScript => GetSqliteScript();

    private static string GetSqlServerScript()
    {
        return @"
-- Increase validation_regex column length to NVARCHAR(MAX) in SQL Server
-- Only alter if current max length is not already max/unlimited
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'settings'
      AND COLUMN_NAME = 'validation_regex'
      AND DATA_TYPE IN ('nvarchar','varchar','nchar','char')
      AND (CHARACTER_MAXIMUM_LENGTH IS NOT NULL AND CHARACTER_MAXIMUM_LENGTH <> -1) -- -1 already MAX
)
BEGIN
    PRINT 'Altering settings.validation_regex column length to NVARCHAR(MAX)...';
    ALTER TABLE settings 
    ALTER COLUMN validation_regex NVARCHAR(MAX) NULL; -- keep NULLability (property is nullable)
    PRINT 'Successfully increased validation_regex column length to NVARCHAR(MAX)';
END
ELSE
BEGIN
    PRINT 'validation_regex column either does not exist or is already NVARCHAR(MAX)';
END
";
    }

    private static string GetSqliteScript()
    {
        return @"
-- SQLite does not enforce VARCHAR length constraints, so no action is needed
-- The column definition change will be handled by schema updates
-- This is a no-op for SQLite databases
SELECT 'SQLite does not enforce VARCHAR constraints - validation_regex column length is effectively unlimited' as migration_result;
";
    }
}
