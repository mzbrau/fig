using Fig.Client.CustomActions;

namespace Fig.Examples.AspNetApi;

public class MigrateDatabaseAction : ICustomAction
{
    public string Name => "Migrate Database";
    public string ButtonName => "Migrate";
    public string Description => "Migrates the database to the latest version.";
    public IEnumerable<string> SettingsUsed => [nameof(Settings.Location)];
    public async Task<IEnumerable<CustomActionResultModel>> Execute(CancellationToken cancellationToken)
    {
        // Simulate some work being done, such as a database migration.
        await Task.Delay(1, cancellationToken);
        return
        [
            new CustomActionResultModel("Database Migration")
            {
                TextResult = "The database migration was successful.",
                DataGridResult =
                [
                    new()
                    {
                        { "Migration", "Initial Migration" },
                        { "Status", "Completed" },
                        { "Timestamp", DateTime.UtcNow }
                    },
                    new()
                    {
                        { "Migration", "Add New Table" },
                        { "Status", "Completed" },
                        { "Timestamp", DateTime.UtcNow.AddMinutes(-5) }
                    }
                ]
            }
        ];
    }
}