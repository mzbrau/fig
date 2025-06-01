using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;
using Fig.Contracts.Status; // For ClientRunSessionDataContract

namespace Fig.Web.Facades
{
    public interface ICustomActionFacade
    {
        Task<CustomActionExecutionResponseDataContract?> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request);
        
        Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId);
        
        Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(string clientName, string customActionId, DateTime startTime, DateTime endTime);
    }
}
