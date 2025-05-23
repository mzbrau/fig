namespace Fig.Web.Models.TimeMachine;

public class CheckPointModel
{
    public CheckPointModel(Guid id, 
        Guid dataId,
        DateTime timestamp,
        int numberOfClients,
        int numberOfSettings,
        string afterEvent,
        string? note,
        string? user)
    {
        Id = id;
        DataId = dataId;
        Timestamp = timestamp;
        NumberOfClients = numberOfClients;
        NumberOfSettings = numberOfSettings;
        AfterEvent = afterEvent;
        Note = note;
        User = user;
    }

    public Guid Id { get; }
    
    public Guid DataId { get; }
    
    public DateTime Timestamp { get; }

    public int NumberOfClients { get; }
    
    public int NumberOfSettings { get; }
    
    public string AfterEvent { get; }
    
    public string? Note { get; }
    
    public string? User { get; }
    
    public bool ApplyInProgress { get; set; }
}