using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models;

public class IntSettingConfigurationModel : SettingConfigurationModel<int>
{
    public IntSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
        DefaultValue = dataContract.DefaultValue ?? default;
    }

    public int UpdatedValue { get; set; }

    public int ConfirmUpdatedValue { get; set; }

    public override dynamic GetValue()
    {
        return Value;
    }

    protected override bool IsUpdatedSecretValueValid()
    {
        return UpdatedValue == ConfirmUpdatedValue;
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        var clone = new IntSettingConfigurationModel(_definitionDataContract, parent)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}