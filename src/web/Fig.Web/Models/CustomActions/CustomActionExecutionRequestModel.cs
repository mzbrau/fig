using System;
using Fig.Contracts.CustomActions; // For CustomActionExecutionRequestDataContract

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionExecutionRequestModel
    {
        public string CustomActionName { get; set; }
        public Guid? RunSessionId { get; set; }

        public CustomActionExecutionRequestModel(string customActionName, Guid? runSessionId = null)
        {
            CustomActionName = customActionName;
            RunSessionId = runSessionId;
        }

        public CustomActionExecutionRequestDataContract ToDataContract()
        {
            return new CustomActionExecutionRequestDataContract(CustomActionName, RunSessionId);
        }
    }
}
