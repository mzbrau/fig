using System;
using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionClientExecuteRequestDataContract
    {
        public Guid ExecutionId { get; set; }
        
        public List<CustomActionResultDataContract> Results { get; set; }

        public bool Success { get; set; }
    }
}
