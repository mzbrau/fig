using System.Runtime.CompilerServices;
using Fig.Client.CustomActions;

namespace Fig.Examples.AspNetApi;

public class MigrateDatabaseAction : ICustomAction
{
    public string Name => "Migrate Database";
    public string ButtonName => "Migrate";
    public string Description => "Migrates the database to the latest version.";
    public IEnumerable<string> SettingsUsed => [nameof(Settings.Location)];
    public async IAsyncEnumerable<CustomActionResultModel> Execute([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Simulate some work being done, such as a database migration.
        await Task.Delay(1, cancellationToken);
        
        List<MigrationResult> migrationResults = [
            new("Initial Migration", "Completed", DateTime.UtcNow), 
            new("Add New Table", "Completed", DateTime.UtcNow.AddMinutes(-5)),
            new("Update Schema", "Completed", DateTime.UtcNow.AddMinutes(-10)),
        ];
        
        yield return ResultBuilder.CreateSuccessResult("Database Migration")
            .WithTextResult("The database migration has been completed successfully.")
            .WithDataGridResult(migrationResults);
    }
}

public class MigrationResult
{
    public MigrationResult(string migration, string status, DateTime timestamp)
    {
        Migration = migration;
        Status = status;
        Timestamp = timestamp;
    }

    public string Migration { get; }
    
    public string Status { get; }
    
    public DateTime Timestamp { get; }
}
