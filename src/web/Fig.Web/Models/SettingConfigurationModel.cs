using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models
{
    public abstract class SettingConfigurationModel
    {
        internal SettingConfigurationModel()
        {
        }
        
        internal SettingConfigurationModel(SettingDefinitionDataContract dataContract)
        {
            Name = dataContract.Name;
            Description = dataContract.Description;
            ValidationType = dataContract.ValidationType;
            ValidationRegex = dataContract.ValidationRegex;
            ValidationExplanation = dataContract.ValidationExplanation;
            Group = dataContract.Group;
            DisplayOrder = dataContract.DisplayOrder;
        }
        
        public string Name { get; set; }

        public string Description { get; set; }

        public ValidationType ValidationType { get; set; }

        public string ValidationRegex { get; set; }
        
        public string ValidationExplanation { get; set; }

        public string Group { get; set; }
 
        public int? DisplayOrder { get; set; }

        public abstract dynamic GetValue();
    }
}
