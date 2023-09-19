using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class BoolSettingConfigurationModel : SettingConfigurationModel<bool>
{
    public BoolSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, bool isReadOnly)
        : base(dataContract, parent, isReadOnly)
    {
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new BoolSettingConfigurationModel(DefinitionDataContract, parent, isReadOnly)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}