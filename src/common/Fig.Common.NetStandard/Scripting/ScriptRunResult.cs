using System;

namespace Fig.Common.NetStandard.Scripting;

public class ScriptRunResult
{
    public bool Success { get; }

    /// <summary>
    /// True when the script was not executed (empty script or infinite-loop guard).
    /// Distinct from <see cref="Success"/> so callers can tally skipped vs. succeeded.
    /// </summary>
    public bool WasSkipped { get; }
    
    public string? ErrorMessage { get; }
    
    public string? ClientName { get; }

    public Exception? Exception { get; }

    /// <summary>
    /// Wall-clock time spent executing this script, in milliseconds. Zero when skipped.
    /// </summary>
    public long DurationMs { get; }

    private ScriptRunResult(bool success, bool wasSkipped, string? clientName, string? errorMessage, Exception? exception, long durationMs)
    {
        Success = success;
        WasSkipped = wasSkipped;
        ClientName = clientName;
        ErrorMessage = errorMessage;
        Exception = exception;
        DurationMs = durationMs;
    }

    public static ScriptRunResult Succeeded(string clientName, long durationMs = 0) =>
        new(true, false, clientName, null, null, durationMs);

    public static ScriptRunResult Failed(string clientName, Exception exception, long durationMs = 0) =>
        new(false, false, clientName, exception.Message, exception, durationMs);

    public static ScriptRunResult Skipped(long durationMs = 0) =>
        new(true, true, null, null, null, durationMs);
}
