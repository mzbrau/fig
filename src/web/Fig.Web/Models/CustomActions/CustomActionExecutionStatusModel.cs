using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Contracts.CustomActions; // For CustomActionExecutionStatusDataContract and ExecutionStatus

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionExecutionStatusModel
    {
        public Guid ExecutionId { get; set; }
        public ExecutionStatus Status { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public List<CustomActionResultModel> Results { get; set; } = new();
        public bool Succeeded { get; set; }
        public bool IsInProgress => Status == ExecutionStatus.Submitted || Status == ExecutionStatus.SentToClient;
        public bool IsCompleted => Status == ExecutionStatus.Completed && Succeeded;
        public bool IsFailed => Status == ExecutionStatus.Completed && !Succeeded;

        public CustomActionExecutionStatusModel(CustomActionExecutionStatusDataContract contract)
        {
            ExecutionId = contract.ExecutionId;
            Status = contract.Status;
            RequestedAt = contract.RequestedAt;
            ExecutedAt = contract.ExecutedAt;
            Succeeded = contract.Succeeded;
            if (contract.Results != null)
            {
                Results = contract.Results.Select(r => new CustomActionResultModel(r)).ToList();
            }
        }
        
        // For placeholder/initial state
        public CustomActionExecutionStatusModel(Guid executionId, ExecutionStatus initialStatus)
        {
            ExecutionId = executionId;
            Status = initialStatus;
            RequestedAt = DateTime.UtcNow; // Or null if not yet requested
        }
    }
}
