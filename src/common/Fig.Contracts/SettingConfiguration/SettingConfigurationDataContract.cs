using System.Collections.Generic;

namespace Fig.Contracts.SettingConfiguration
{
    public class SettingConfigurationDataContract
    {
        public string Name { get; set; }

        public string FriendlyName { get; set; }
        
        public string Description { get; set; }
        
        public bool IsSecret { get; set; }

        // Will be encrypted if IsSecret is true.
        public dynamic Value { get; set; }
        
        // Not encrypted, inwards communication only.
        public dynamic DefaultValue { get; set; }
        
        public ValidationType ValidationType { get; set; }

        public string ValidationRegex { get; set; }
        
        public string ValidationExplanation { get; set; }
        
        public List<string> ValidValues { get; set; }

        public string Group { get; set; }
 
        public int? DisplayOrder { get; set; }
    }
}