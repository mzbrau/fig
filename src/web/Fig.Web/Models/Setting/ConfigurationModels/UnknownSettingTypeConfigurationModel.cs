using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class UnknownSettingTypeConfigurationModel : SettingConfigurationModel<string>
{
    public UnknownSettingTypeConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        return new UnknownSettingTypeConfigurationModel(DefinitionDataContract, parent);
    }
}