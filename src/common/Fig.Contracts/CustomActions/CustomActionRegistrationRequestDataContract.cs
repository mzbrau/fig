using System.Collections.Generic;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionRegistrationRequestDataContract
    {
        public string ClientName { get; set; }
        public string? Instance { get; set; }
        public List<CustomActionDefinitionDataContract> CustomActions { get; set; }
    }
}
