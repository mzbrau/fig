using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Contracts.CustomActions; // For CustomActionExecutionStatusDataContract

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionExecutionStatusModel
    {
        public Guid ExecutionId { get; set; }
        public string Status { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<CustomActionResultModel> Results { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool IsInProgress => Status == "Pending" || Status == "Executing";
        public bool IsCompleted => Status == "Completed";
        public bool IsFailed => Status == "Failed";

        public CustomActionExecutionStatusModel(CustomActionExecutionStatusDataContract contract)
        {
            ExecutionId = contract.ExecutionId;
            Status = contract.Status;
            RequestedAt = contract.RequestedAt;
            ExecutedAt = contract.ExecutedAt;
            CompletedAt = contract.CompletedAt;
            ErrorMessage = contract.ErrorMessage;
            if (contract.Results != null)
            {
                Results = contract.Results.Select(r => new CustomActionResultModel(r)).ToList();
            }
        }
        
        // For placeholder/initial state
        public CustomActionExecutionStatusModel(Guid executionId, string initialStatus)
        {
            ExecutionId = executionId;
            Status = initialStatus;
            RequestedAt = DateTime.UtcNow; // Or null if not yet requested
        }
    }
}
