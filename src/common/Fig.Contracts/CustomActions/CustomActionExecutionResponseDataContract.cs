using System;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionResponseDataContract
    {
        public CustomActionExecutionResponseDataContract(Guid executionId, string message, bool executionPending)
        {
            ExecutionId = executionId;
            Message = message;
            ExecutionPending = executionPending;
        }

        public Guid ExecutionId { get; }
        
        public string Message { get; }
        
        public bool ExecutionPending { get; }
    }
}
