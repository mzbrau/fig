namespace Fig.WebHooks.Contracts;

public class MinRunSessionsDataContract : IWebHookContract
{
    public MinRunSessionsDataContract(string clientName, string? instance, int runSessions, RunSessionsEvent runSessionsEvent, Uri? link, bool isTest = false)
    {
        ClientName = clientName;
        Instance = instance;
        RunSessions = runSessions;
        RunSessionsEvent = runSessionsEvent;
        Link = link;
        IsTest = isTest;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public int RunSessions { get; set; }
    
    public RunSessionsEvent RunSessionsEvent { get; set; }
    
    public Uri? Link { get; set; }
    
    public bool IsTest { get; }
}