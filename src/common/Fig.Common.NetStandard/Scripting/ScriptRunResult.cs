namespace Fig.Common.NetStandard.Scripting;

public class ScriptRunResult
{
    public bool Success { get; }
    
    public string? ErrorMessage { get; }
    
    public string? ClientName { get; }

    private ScriptRunResult(bool success, string? clientName, string? errorMessage)
    {
        Success = success;
        ClientName = clientName;
        ErrorMessage = errorMessage;
    }

    public static ScriptRunResult Succeeded(string clientName) => new(true, clientName, null);

    public static ScriptRunResult Failed(string clientName, string errorMessage) => new(false, clientName, errorMessage);

    public static ScriptRunResult Skipped() => new(true, null, null);
}
