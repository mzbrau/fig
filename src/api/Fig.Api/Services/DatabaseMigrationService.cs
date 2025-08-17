using Fig.Api.DatabaseMigrations;
using Fig.Api.Datalayer.Repositories;
using System.Diagnostics;

namespace Fig.Api.Services;

public interface IDatabaseMigrationService
{
    Task RunMigrationsAsync();
}

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDatabaseMigrationRepository _migrationRepository;
    private readonly IEnumerable<IDatabaseMigration> _migrations;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        IDatabaseMigrationRepository migrationRepository,
        IEnumerable<IDatabaseMigration> migrations,
        ILogger<DatabaseMigrationService> logger)
    {
        _migrationRepository = migrationRepository;
        _migrations = migrations;
        _logger = logger;
    }

    public async Task RunMigrationsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting database migration check...");

        try
        {
            // Validate migration sequence
            ValidateMigrationSequence();

            await RunMigrationsInternal();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run database migrations");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("Database migration check completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task RunMigrationsInternal()
    {
        var executedMigrations = await _migrationRepository.GetExecutedMigrations();
        var executedNumbers = new HashSet<int>(executedMigrations
            .Where(m => m.Status == "complete")
            .Select(m => m.ExecutionNumber));

        var pendingMigrations = _migrations
            .Where(m => !executedNumbers.Contains(m.ExecutionNumber))
            .OrderBy(m => m.ExecutionNumber)
            .ToList();

        if (!pendingMigrations.Any())
        {
            _logger.LogInformation("No pending migrations found");
            return;
        }

        _logger.LogInformation("Found {Count} pending migrations", pendingMigrations.Count);

        foreach (var migration in pendingMigrations)
        {
            await ExecuteMigration(migration);
        }

        _logger.LogInformation("All migrations completed successfully");
    }

    private async Task ExecuteMigration(IDatabaseMigration migration)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing migration {ExecutionNumber}: {Description}", 
            migration.ExecutionNumber, migration.Description);

        try
        {
            // Attempt to mark as pending (another instance may already be doing it)
            if (!await _migrationRepository.TryBeginMigration(migration.ExecutionNumber, migration.Description))
            {
                _logger.LogInformation("Migration {ExecutionNumber} is being handled by another instance or already complete", migration.ExecutionNumber);
                return;
            }

            try
            {
                // Get the appropriate script for the database type
                var script = await _migrationRepository.GetScriptForDatabase(migration);

                if (string.IsNullOrWhiteSpace(script))
                {
                    _logger.LogWarning("Migration {ExecutionNumber} has no script for the current database type. Skipping", 
                        migration.ExecutionNumber);
                    await _migrationRepository.CompleteMigration(migration.ExecutionNumber, TimeSpan.Zero);
                    return;
                }

                // Execute migration in its own transaction
                await _migrationRepository.ExecuteRawSql(script);

                stopwatch.Stop();
                await _migrationRepository.CompleteMigration(migration.ExecutionNumber, stopwatch.Elapsed);
                _logger.LogInformation("Migration {ExecutionNumber} completed successfully in {ElapsedMs}ms", 
                    migration.ExecutionNumber, stopwatch.ElapsedMilliseconds);
            }
            catch
            {
                await _migrationRepository.FailMigration(migration.ExecutionNumber);
                throw;
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Migration {ExecutionNumber} failed after {ElapsedMs}ms: {Description}. " +
                           "Rolling back and aborting further migrations", 
                migration.ExecutionNumber, stopwatch.ElapsedMilliseconds, migration.Description);
            
            throw new InvalidOperationException(
                $"Migration {migration.ExecutionNumber} failed: {migration.Description}. " +
                $"Error: {ex.Message}", ex);
        }
    }

    private void ValidateMigrationSequence()
    {
        var sortedMigrations = _migrations.OrderBy(m => m.ExecutionNumber).ToList();
        
        for (int i = 0; i < sortedMigrations.Count; i++)
        {
            var expectedNumber = i + 1;
            var actualNumber = sortedMigrations[i].ExecutionNumber;
            
            if (actualNumber != expectedNumber)
            {
                throw new InvalidOperationException(
                    $"Migration sequence validation failed. Expected migration number {expectedNumber}, " +
                    $"but found {actualNumber}. Migrations must be sequential starting from 1.");
            }
        }

        _logger.LogDebug("Migration sequence validation passed. Found {Count} migrations", sortedMigrations.Count);
    }

    // Wait logic no longer required with pending/complete status rows
}
