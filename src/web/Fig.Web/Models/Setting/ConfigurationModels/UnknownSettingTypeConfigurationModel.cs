using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class UnknownSettingTypeConfigurationModel : SettingConfigurationModel<string>
{
    public UnknownSettingTypeConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, bool isReadOnly)
        : base(dataContract, parent, isReadOnly)
    {
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        return new UnknownSettingTypeConfigurationModel(DefinitionDataContract, parent, isReadOnly);
    }
}