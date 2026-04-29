using Fig.Api.DatabaseMigrations;

namespace Fig.Api.DatabaseMigrations.Migrations;

public class Migration_006_BackfillCustomActionClassification : IDatabaseMigration
{
    public int ExecutionNumber => 6;

    public string Description => "Backfill custom action classifications to Technical";

    public string SqlServerScript =>
        """
        UPDATE custom_actions
        SET classification = 0
        WHERE classification IS NULL;
        """;

    public string SqliteScript =>
        """
        UPDATE custom_actions
        SET classification = 0
        WHERE classification IS NULL;
        """;
}
