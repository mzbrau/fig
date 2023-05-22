namespace Fig.Web.Models.Clients;

public class MemoryUsageModel
{
    public MemoryUsageModel(int clientRunTimeSeconds, long memoryUsageBytes)
    {
        ClientRunTimeSeconds = clientRunTimeSeconds;
        MemoryUsageMegaBytes = memoryUsageBytes / 1024 / 1024;
    }

    public int ClientRunTimeSeconds { get; }
    
    public long MemoryUsageMegaBytes { get; }
}