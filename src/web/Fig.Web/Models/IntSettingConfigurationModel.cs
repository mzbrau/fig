using Fig.Contracts.SettingConfiguration;

namespace Fig.Web.Models
{
    public class IntSettingConfigurationModel : SettingConfigurationModel
    {
        public IntSettingConfigurationModel()
        {
            
        }
        
        public IntSettingConfigurationModel(SettingConfigurationDataContract dataContract) 
            : base(dataContract)
        {
            Value = dataContract.Value;
        }
        
        public int Value { get; set; }
        public override dynamic GetValue()
        {
            return Value;
        }
    }
}
