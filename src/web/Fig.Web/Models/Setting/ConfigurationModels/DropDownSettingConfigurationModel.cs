using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class DropDownSettingConfigurationModel : SettingConfigurationModel<string>
{
    public DropDownSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
        ValidValues = dataContract.ValidValues!;
    }

    public List<string> ValidValues { get; }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        var clone = new DropDownSettingConfigurationModel(DefinitionDataContract, parent)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}