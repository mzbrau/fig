using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models
{
    public class IntSettingConfigurationModel : SettingConfigurationModel
    {
        public IntSettingConfigurationModel()
        {
            
        }
        
        public IntSettingConfigurationModel(SettingDefinitionDataContract dataContract) 
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
