namespace Fig.Web.Models.Setting;

public class VerificationResultModel
{
    public VerificationResultModel(bool success, string? message, List<string> logs, DateTime executionTime)
    {
        Success = success;
        Message = message;
        Logs = logs;
        ExecutionTime = executionTime;
    }

    public VerificationResultModel(string message)
    {
        Message = message;
    }

    public bool Success { get; }

    public string? Message { get; }

    public DateTime ExecutionTime { get; }

    public List<string> Logs { get; } = new();
}