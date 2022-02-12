using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models;

public class DropDownSettingConfigurationModel : SettingConfigurationModel<string>
{
    public DropDownSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
        ValidValues = dataContract.ValidValues;
        DefaultValue = dataContract.DefaultValue ?? ValidValues.FirstOrDefault();
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