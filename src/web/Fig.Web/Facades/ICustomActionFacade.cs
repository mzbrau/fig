using Fig.Contracts.CustomActions;

namespace Fig.Web.Facades
{
    public interface ICustomActionFacade
    {
        Task<CustomActionExecutionResponseDataContract?> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request);
        
        Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId);
        
        Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(string clientName, string customActionId, DateTime startTime, DateTime endTime);
    }
}
