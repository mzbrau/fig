using Fig.Api.DatabaseMigrations;

namespace Fig.Api.DatabaseMigrations.Migrations;

public class Migration_008_AddUserPasswordChangeRequired : IDatabaseMigration
{
    public int ExecutionNumber => 8;

    public string Description => "Backfill password change required flag to false for existing users";

    public string SqlServerScript =>
        """
        UPDATE users
        SET password_change_required = 0
        WHERE password_change_required IS NULL;
        """;

    public string SqliteScript =>
        """
        UPDATE users
        SET password_change_required = 0
        WHERE password_change_required IS NULL;
        """;
}
