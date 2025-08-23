namespace Fig.WebHooks.Contracts;

public class MinRunSessionsDataContract : IWebHookContract
{
    public MinRunSessionsDataContract(string clientName, string? instance, int runSessions, RunSessionsEvent runSessionsEvent, Uri? link, bool isTest = false)
    {
        if (clientName == null)
            throw new ArgumentNullException(nameof(clientName));
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("Client name cannot be empty or whitespace.", nameof(clientName));
        if (runSessions < 0)
            throw new ArgumentOutOfRangeException(nameof(runSessions), "Run sessions cannot be negative.");

        ClientName = clientName;
        Instance = instance;
        RunSessions = runSessions;
        RunSessionsEvent = runSessionsEvent;
        Link = link;
        IsTest = isTest;
    }

    public string ClientName { get; init; }
    
    public string? Instance { get; init; }
    
    public int RunSessions { get; init; }
    
    public RunSessionsEvent RunSessionsEvent { get; init; }
    
    public Uri? Link { get; init; }
    
    public bool IsTest { get; init; }
}