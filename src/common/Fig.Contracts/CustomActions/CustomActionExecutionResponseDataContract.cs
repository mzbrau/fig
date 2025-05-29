using System;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionResponseDataContract
    {
        public Guid ExecutionId { get; set; }
        public string Message { get; set; }
    }
}
