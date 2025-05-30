using System;
using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionHistoryDataContract
    {
        public CustomActionExecutionHistoryDataContract(string clientName, string customActionName, List<CustomActionExecutionStatusDataContract> executions)
        {
            ClientName = clientName;
            CustomActionName = customActionName;
            Executions = executions;
        }

        public string ClientName { get; set; }
        public string CustomActionName { get; set; }
        public List<CustomActionExecutionStatusDataContract> Executions { get; set; }
    }
}
