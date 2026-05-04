using Fig.Api.DatabaseMigrations;

namespace Fig.Api.DatabaseMigrations.Migrations;

public class Migration_007_EnableMigrateFromMigrationsByDefault : IDatabaseMigration
{
    public int ExecutionNumber => 7;

    public string Description => "Backfill migrate-from migrations to enabled for existing configuration rows";

    public string SqlServerScript =>
        """
        UPDATE configuration
        SET allow_migrate_from_migrations = 1
        WHERE allow_migrate_from_migrations IS NULL OR allow_migrate_from_migrations = 0;
        """;

    public string SqliteScript =>
        """
        UPDATE configuration
        SET allow_migrate_from_migrations = 1
        WHERE allow_migrate_from_migrations IS NULL OR allow_migrate_from_migrations = 0;
        """;
}
