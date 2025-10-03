namespace Fig.Api.Services;

/// <summary>
/// Service for performing data cleanup operations on old records in the database.
/// </summary>
public interface IDataCleanupService
{
    /// <summary>
    /// Performs cleanup of old data based on the configured cleanup settings.
    /// </summary>
    /// <returns>The total number of records deleted across all cleanup operations.</returns>
    Task<int> PerformCleanupAsync();
}
