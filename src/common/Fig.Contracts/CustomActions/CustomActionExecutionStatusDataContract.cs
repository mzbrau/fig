using System;
using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionStatusDataContract
    {
        public CustomActionExecutionStatusDataContract(Guid executionId,
            ExecutionStatus status,
            DateTime? requestedAt,
            DateTime? executedAt,
            List<CustomActionResultDataContract>? results,
            bool succeeded,
            Guid? executedByRunSession)
        {
            ExecutionId = executionId;
            Status = status;
            RequestedAt = requestedAt;
            ExecutedAt = executedAt;
            Results = results;
            Succeeded = succeeded;
            ExecutedByRunSession = executedByRunSession;
        }

        public Guid ExecutionId { get; }
        public ExecutionStatus Status { get; }
        public DateTime? RequestedAt { get; }
        public DateTime? ExecutedAt { get; }
        public List<CustomActionResultDataContract>? Results { get; }
        
        public bool Succeeded { get; }
        
        public Guid? ExecutedByRunSession { get; }
    }
}
