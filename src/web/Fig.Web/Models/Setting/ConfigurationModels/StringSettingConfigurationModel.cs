using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class StringSettingConfigurationModel : SettingConfigurationModel<string?>
{
    public StringSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
    }

    public string ConfirmUpdatedValue { get; set; } = string.Empty;

    protected override bool IsUpdatedSecretValueValid()
    {
        return !string.IsNullOrWhiteSpace(UpdatedValue) &&
               UpdatedValue == ConfirmUpdatedValue;
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        var clone = new StringSettingConfigurationModel(DefinitionDataContract, parent)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}