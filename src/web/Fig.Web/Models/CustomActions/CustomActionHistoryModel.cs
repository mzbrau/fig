using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Contracts.CustomActions; // For CustomActionExecutionHistoryDataContract

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionHistoryModel
    {
        public string ClientName { get; set; }
        public string CustomActionName { get; set; }
        public List<CustomActionExecutionStatusModel> Executions { get; set; } = new();

        public CustomActionHistoryModel(CustomActionExecutionHistoryDataContract contract)
        {
            ClientName = contract.ClientName;
            CustomActionName = contract.CustomActionName;
            if (contract.Executions != null)
            {
                Executions = contract.Executions.Select(e => new CustomActionExecutionStatusModel(e)).ToList();
            }
        }
    }
}
