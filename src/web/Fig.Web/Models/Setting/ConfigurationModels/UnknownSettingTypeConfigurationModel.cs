using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class UnknownSettingTypeConfigurationModel : SettingConfigurationModel<string>
{
    public UnknownSettingTypeConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
    }

    public override string IconKey => "settings";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        return new UnknownSettingTypeConfigurationModel(DefinitionDataContract, parent, Presentation);
    }
}