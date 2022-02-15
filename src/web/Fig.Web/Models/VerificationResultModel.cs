namespace Fig.Web.Models;

public class VerificationResultModel
{
    public bool Success { get; set; }

    public string Message { get; set; }

    public string RequestingUser { get; set; }

    public DateTime ExecutionTime { get; set; }

    public List<string> Logs { get; set; } = new();
}