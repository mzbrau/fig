using System;

namespace Fig.Contracts.CheckPoint;

public class CheckPointDataContract
{
    public CheckPointDataContract(Guid dataId,
        DateTime timestamp,
        int numberOfClients,
        int numberOfSettings,
        string afterEvent)
    {
        DataId = dataId;
        Timestamp = timestamp;
        NumberOfClients = numberOfClients;
        NumberOfSettings = numberOfSettings;
        AfterEvent = afterEvent;
    }

    public Guid DataId { get; }
    
    public DateTime Timestamp { get; }

    public int NumberOfClients { get; }
    
    public int NumberOfSettings { get; }
    
    public string AfterEvent { get; }
}