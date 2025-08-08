using Fig.Api.DatabaseMigrations;

namespace Fig.Api.DatabaseMigrations.Migrations;

public class Migration_001_IncreaseValidationRegexLength : IDatabaseMigration
{
    public int ExecutionNumber => 1;
    
    public string Description => "Increase validation_regex column length from 256 to 5012 characters";
    
    public string SqlServerScript => GetSqlServerScript();
    
    public string SqliteScript => GetSqliteScript();

    private static string GetSqlServerScript()
    {
        return @"
-- Increase validation_regex column length from 256 to 5012 characters in SQL Server
-- First check if the column exists and needs to be altered
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'setting' 
    AND COLUMN_NAME = 'validation_regex'
    AND CHARACTER_MAXIMUM_LENGTH < 5012
)
BEGIN
    ALTER TABLE setting 
    ALTER COLUMN validation_regex NVARCHAR(5012);
    
    PRINT 'Successfully increased validation_regex column length to 5012 characters';
END
ELSE
BEGIN
    PRINT 'validation_regex column either does not exist or is already 5012+ characters';
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
