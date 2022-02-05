using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models;

public class DropDownSettingConfigurationModel : SettingConfigurationModel
{
    public DropDownSettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<bool, string> valueChanged)
        : base(dataContract, valueChanged)
    {
        Value = dataContract.Value;
        ValidValues = dataContract.ValidValues;
    }

    public List<string> ValidValues { get; set; }

    public string Value { get; set; }

    public string UpdatedValue { get; set; }

    public override dynamic GetValue()
    {
        return Value;
    }

    protected override void ApplyUpdatedSecretValue()
    {
        Value = UpdatedValue;
    }

    protected override bool IsUpdatedSecretValueValid()
    {
        return true;
    }

    internal override SettingConfigurationModel Clone()
    {
        var clone = new DropDownSettingConfigurationModel(_definitionDataContract, _valueChanged)
        {
            IsDirty = true
        };

        return clone;
    }
}