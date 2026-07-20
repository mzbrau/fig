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

    private ScriptRunResult(bool success, bool wasSkipped, string? clientName, string? errorMessage, Exception? exception)
    {
        Success = success;
        WasSkipped = wasSkipped;
        ClientName = clientName;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static ScriptRunResult Succeeded(string clientName) => new(true, false, clientName, null, null);

    public static ScriptRunResult Failed(string clientName, Exception exception) => new(false, false, clientName, exception.Message, exception);

    public static ScriptRunResult Skipped() => new(true, true, null, null, null);
}
