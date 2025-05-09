using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class BoolSettingConfigurationModel : SettingConfigurationModel<bool>
{
    public BoolSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
    }

    public override string IconKey => "toggle_on";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new BoolSettingConfigurationModel(DefinitionDataContract, parent, _presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}