using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models;

public class DropDownSettingConfigurationModel : SettingConfigurationModel
{
    public DropDownSettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<SettingEvent> stateChanged)
        : base(dataContract, stateChanged)
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

    internal override SettingConfigurationModel Clone(Action<SettingEvent> stateChanged)
    {
        var clone = new DropDownSettingConfigurationModel(_definitionDataContract, stateChanged)
        {
            IsDirty = true
        };

        return clone;
    }
}