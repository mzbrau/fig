using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models;

public class StringSettingConfigurationModel : SettingConfigurationModel<string>
{
    public StringSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
        DefaultValue = dataContract.DefaultValue ?? string.Empty;
    }

    public string UpdatedValue { get; set; }

    public string ConfirmUpdatedValue { get; set; }

    public override dynamic GetValue()
    {
        return Value;
    }

    protected override bool IsUpdatedSecretValueValid()
    {
        return !string.IsNullOrWhiteSpace(UpdatedValue) &&
               UpdatedValue == ConfirmUpdatedValue;
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        var clone = new StringSettingConfigurationModel(_definitionDataContract, parent)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}