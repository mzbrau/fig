using System;
using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionStatusDataContract
    {
        public Guid ExecutionId { get; set; }
        public string Status { get; set; } // e.g., "Pending", "Executing", "Completed", "Failed"
        public DateTime? RequestedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<CustomActionResultDataContract>? Results { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
