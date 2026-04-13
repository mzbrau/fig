using System;

namespace Fig.Common.NetStandard.Scripting;

public class ScriptRunResult
{
    public bool Success { get; }
    
    public string? ErrorMessage { get; }
    
    public string? ClientName { get; }

    public Exception? Exception { get; }

    private ScriptRunResult(bool success, string? clientName, string? errorMessage, Exception? exception)
    {
        Success = success;
        ClientName = clientName;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static ScriptRunResult Succeeded(string clientName) => new(true, clientName, null, null);

    public static ScriptRunResult Failed(string clientName, Exception exception) => new(false, clientName, exception.Message, exception);

    public static ScriptRunResult Skipped() => new(true, null, null, null);
}
