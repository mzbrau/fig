using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;

namespace Fig.Api.Services
{
    public interface ICustomActionService
    {
        Task RegisterCustomActions(CustomActionRegistrationRequestDataContract request, CancellationToken cancellationToken);
        Task<IEnumerable<CustomActionDefinitionDataContract>> GetCustomActions(string clientName, string? instance, CancellationToken cancellationToken);
        Task<CustomActionExecutionResponseDataContract> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request, CancellationToken cancellationToken);
        Task<IEnumerable<CustomActionClientPollResponseDataContract>> PollForExecutionRequests(string clientName, string? instance, CancellationToken cancellationToken);
        Task SubmitExecutionResults(CustomActionClientExecuteRequestDataContract request, CancellationToken cancellationToken);
        Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId, CancellationToken cancellationToken);
        Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(Guid customActionId, int limit, int offset, CancellationToken cancellationToken);
        Task DeleteClientCustomActions(Guid settingClientId, CancellationToken cancellationToken);
    }
}
