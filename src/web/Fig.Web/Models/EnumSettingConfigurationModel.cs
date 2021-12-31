using Fig.Contracts.SettingConfiguration;

namespace Fig.Web.Models;

public class EnumSettingConfigurationModel : SettingConfigurationModel
{
    public EnumSettingConfigurationModel(SettingConfigurationDataContract dataContract) 
        : base(dataContract)
    {
        Value = dataContract.Value;
        ValidValues = dataContract.ValidValues;
    }
    
    public List<string> ValidValues { get; set; }
    
    public string Value { get; set; }

    public override dynamic GetValue()
    {
        return Value;
    }
}