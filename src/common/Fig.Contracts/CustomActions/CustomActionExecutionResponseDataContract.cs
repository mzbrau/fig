using System;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionResponseDataContract
    {
        public CustomActionExecutionResponseDataContract(Guid executionId, string message)
        {
            ExecutionId = executionId;
            Message = message;
        }

        public Guid ExecutionId { get; }
        
        public string Message { get; }
    }
}
