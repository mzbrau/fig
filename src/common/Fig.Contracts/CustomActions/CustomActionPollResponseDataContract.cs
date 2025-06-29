using System;
using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionPollResponseDataContract
    {
        public CustomActionPollResponseDataContract(Guid requestId, string customActionToExecute)
        {
            RequestId = requestId;
            CustomActionToExecute = customActionToExecute;
        }

        public Guid RequestId { get; set; }
        
        public string CustomActionToExecute { get; set; }
    }
}
