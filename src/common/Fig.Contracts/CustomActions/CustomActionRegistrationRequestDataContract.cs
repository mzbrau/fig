using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionRegistrationRequestDataContract
    {
        public CustomActionRegistrationRequestDataContract(string clientName, List<CustomActionDefinitionDataContract> customActions)
        {
            ClientName = clientName;
            CustomActions = customActions;
        }

        public string ClientName { get; set; }
        public List<CustomActionDefinitionDataContract> CustomActions { get; set; }
    }
}
