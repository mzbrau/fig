namespace Fig.Web.Models.Setting;

public class VerificationResultModel
{
    public VerificationResultModel(bool success, string? message, List<string> logs, DateTime executionTime, string? requestingUser)
    {
        Success = success;
        Message = message;
        Logs = logs;
        ExecutionTime = executionTime;
        RequestingUser = requestingUser;
    }

    public VerificationResultModel(string message)
    {
        Message = message;
    }

    public bool Success { get; }

    public string? Message { get; }

    public DateTime ExecutionTime { get; }
    
    public string? RequestingUser { get; }

    public List<string> Logs { get; } = new();
}