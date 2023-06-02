namespace Fig.WebHooks.Contracts;

public class MinRunSessionsDataContract
{
    public MinRunSessionsDataContract(string clientName, string? instance, int runSessions, RunSessionsEvent runSessionsEvent, Uri? link)
    {
        ClientName = clientName;
        Instance = instance;
        RunSessions = runSessions;
        RunSessionsEvent = runSessionsEvent;
        Link = link;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public int RunSessions { get; set; }
    
    public RunSessionsEvent RunSessionsEvent { get; set; }
    
    public Uri? Link { get; set; }
}