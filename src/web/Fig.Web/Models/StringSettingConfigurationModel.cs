using Fig.Contracts.SettingConfiguration;

namespace Fig.Web.Models
{
    public class StringSettingConfigurationModel : SettingConfigurationModel
    {
        public StringSettingConfigurationModel()
        {
            
        }
        
        public StringSettingConfigurationModel(SettingConfigurationDataContract dataContract) 
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
