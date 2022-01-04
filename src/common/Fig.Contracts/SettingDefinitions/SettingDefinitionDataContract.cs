using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingDefinitionDataContract
    {
        public string Name { get; set; }

        public string FriendlyName { get; set; }
        
        public string Description { get; set; }
        
        public bool IsSecret { get; set; }

        public dynamic Value { get; set; }
        
        public dynamic DefaultValue { get; set; }

        public ValidationType ValidationType { get; set; }

        public string ValidationRegex { get; set; }
        
        public string ValidationExplanation { get; set; }
        
        public List<string> ValidValues { get; set; }

        public string Group { get; set; }
 
        public int? DisplayOrder { get; set; }
        
        public bool Advanced { get; set; }
        
        public string? StringFormat { get; set; }
    }
}

