using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models
{
    public class StringSettingConfigurationModel : SettingConfigurationModel
    {
        public StringSettingConfigurationModel()
        {
            
        }
        
        public StringSettingConfigurationModel(SettingDefinitionDataContract dataContract) 
            : base(dataContract)
        {
            Value = dataContract.Value;
            IsSecret = dataContract.IsSecret;
            DefaultValue = dataContract.DefaultValue;
        }
        
        public string Value { get; set; }
        
        public bool IsSecret { get; set; }
        
        public string DefaultValue { get; set; }
        public override dynamic GetValue()
        {
            return Value;
        }
    }
}
