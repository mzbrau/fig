using System;
using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionHistoryDataContract
    {
        public Guid CustomActionId { get; set; }
        public string CustomActionName { get; set; }
        public List<CustomActionExecutionStatusDataContract> Executions { get; set; }
    }
}
