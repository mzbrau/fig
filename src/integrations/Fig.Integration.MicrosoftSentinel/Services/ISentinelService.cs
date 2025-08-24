namespace Fig.Integration.MicrosoftSentinel.Services;

public interface ISentinelService
{
    /// <summary>
    /// Sends a log entry to Microsoft Sentinel
    /// </summary>
    /// <param name="logData">The log data to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SendLogAsync(object logData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to Microsoft Sentinel
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}