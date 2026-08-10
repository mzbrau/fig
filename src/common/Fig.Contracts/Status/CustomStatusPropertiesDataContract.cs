using System.Collections.Generic;

namespace Fig.Contracts.Status
{
    public class CustomStatusPropertiesDataContract
    {
        public CustomStatusPropertiesDataContract()
        {
        }

        public CustomStatusPropertiesDataContract(List<CustomStatusPropertyDataContract> properties)
        {
            Properties = properties;
        }

        public List<CustomStatusPropertyDataContract> Properties { get; set; } = [];
    }
}
