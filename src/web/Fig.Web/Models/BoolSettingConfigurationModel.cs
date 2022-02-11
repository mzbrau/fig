using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models;

public class BoolSettingConfigurationModel : SettingConfigurationModel<bool>
{
    public BoolSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
        DefaultValue = dataContract.DefaultValue ?? false;
    }

    public override dynamic GetValue()
    {
        return Value;
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        var clone = new BoolSettingConfigurationModel(_definitionDataContract, parent)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}