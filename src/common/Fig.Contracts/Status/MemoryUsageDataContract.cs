namespace Fig.Contracts.Status;

public class MemoryUsageDataContract
{
    public MemoryUsageDataContract(int clientRunTimeSeconds, long memoryUsageBytes)
    {
        ClientRunTimeSeconds = clientRunTimeSeconds;
        MemoryUsageBytes = memoryUsageBytes;
    }

    public int ClientRunTimeSeconds { get; }
    
    public long MemoryUsageBytes { get; }
}