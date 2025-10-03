using Fig.Api.DatabaseMigrations;

namespace Fig.Api.DatabaseMigrations.Migrations;

public class Migration_002_DisableTimeMachine : IDatabaseMigration
{
    public int ExecutionNumber => 2;
    
    public string Description => "Disable time machine feature in configuration table";
    
    public string SqlServerScript => GetSqlServerScript();
    
    public string SqliteScript => GetSqliteScript();

    private static string GetSqlServerScript()
    {
        return @"
-- Disable time machine feature in the configuration table
-- Set enable_time_machine to 0 (false) for all rows
UPDATE configuration 
SET enable_time_machine = 0
WHERE enable_time_machine = 1;

PRINT 'Time machine feature disabled in configuration table';
";
    }

    private static string GetSqliteScript()
    {
        return @"
-- Disable time machine feature in the configuration table
-- Set enable_time_machine to 0 (false) for all rows
UPDATE configuration 
SET enable_time_machine = 0
WHERE enable_time_machine = 1;

SELECT 'Time machine feature disabled in configuration table' as migration_result;
";
    }
}
