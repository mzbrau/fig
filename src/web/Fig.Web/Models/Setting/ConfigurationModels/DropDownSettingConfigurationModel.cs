using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class DropDownSettingConfigurationModel : SettingConfigurationModel<string>
{
    public DropDownSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
        ValidValues = dataContract.ValidValues!;
    }

    public List<string> ValidValues { get; set; }

    public override string IconKey => "top_panel_open";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new DropDownSettingConfigurationModel(DefinitionDataContract, parent, _presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}