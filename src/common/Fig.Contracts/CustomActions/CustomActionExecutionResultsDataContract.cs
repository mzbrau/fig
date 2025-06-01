using System;
using System.Collections.Generic;

namespace Fig.Contracts.CustomActions;

public class CustomActionExecutionResultsDataContract
{
    public CustomActionExecutionResultsDataContract(Guid executionId, List<CustomActionResultDataContract> results, bool success)
    {
        ExecutionId = executionId;
        Results = results;
        Success = success;
    }

    public Guid ExecutionId { get; }
        
    public List<CustomActionResultDataContract> Results { get; }

    public bool Success { get; }
    
    public Guid RunSessionId { get; set; }
}