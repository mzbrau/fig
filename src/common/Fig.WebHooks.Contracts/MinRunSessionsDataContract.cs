namespace Fig.WebHooks.Contracts;

public class MinRunSessionsDataContract
{
    public MinRunSessionsDataContract(string clientName, string? instance, int runSessions, RunSessionsEvent runSessionsEvent)
    {
        ClientName = clientName;
        Instance = instance;
        RunSessions = runSessions;
        RunSessionsEvent = runSessionsEvent;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public int RunSessions { get; set; }
    
    public RunSessionsEvent RunSessionsEvent { get; set; }
}