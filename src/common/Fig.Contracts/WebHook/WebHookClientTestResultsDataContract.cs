using System;
using System.Collections.Generic;

namespace Fig.Contracts.WebHook;

public class WebHookClientTestResultsDataContract
{
    public WebHookClientTestResultsDataContract(Guid clientId, string clientName, List<TestResultDataContract> results)
    {
        ClientId = clientId;
        ClientName = clientName;
        Results = results;
    }

    public Guid ClientId { get; set; }
    
    public string ClientName { get; set; }
    
    public List<TestResultDataContract> Results { get; set; }
}