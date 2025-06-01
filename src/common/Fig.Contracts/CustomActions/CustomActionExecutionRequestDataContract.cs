using System;
using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionRequestDataContract
    {
        public CustomActionExecutionRequestDataContract(string customActionName, Guid? runSessionId = null)
        {
            CustomActionName = customActionName;
            RunSessionId = runSessionId;
        }

        public Guid? RunSessionId { get; set; }
        
        public string CustomActionName { get; set; }
    }
}
