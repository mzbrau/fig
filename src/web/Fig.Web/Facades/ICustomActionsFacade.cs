using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;
using Fig.Contracts.Status; // For ClientRunSessionDataContract

namespace Fig.Web.Facades
{
    public interface ICustomActionsFacade
    {
        Task<List<CustomActionDefinitionDataContract>> GetCustomActions(string clientName, string? instance, CancellationToken cancellationToken);
        Task<CustomActionExecutionResponseDataContract> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request, CancellationToken cancellationToken);
        Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId, CancellationToken cancellationToken);
        Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(string clientName, string customActionId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken);
        Task<List<ClientRunSessionDataContract>> GetRunSessions(string clientName, CancellationToken cancellationToken);
    }
}
