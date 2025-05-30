using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;

namespace Fig.Api.Services
{
    public interface ICustomActionService
    {
        Task RegisterCustomActions(string clientSecret, CustomActionRegistrationRequestDataContract request);

        Task<CustomActionExecutionResponseDataContract> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request);
        
        Task<IEnumerable<CustomActionClientPollResponseDataContract>> PollForExecutionRequests(string clientName, Guid runSessionId, string clientSecret);

        Task SubmitExecutionResults(string clientName, string clientSecret,
            CustomActionClientExecuteRequestDataContract request);
        
        Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId);

        Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(string clientName, string customActionName,
            DateTime startTime, DateTime endTime);
    }
}
