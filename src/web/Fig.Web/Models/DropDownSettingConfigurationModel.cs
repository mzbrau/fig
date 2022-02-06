using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;

namespace Fig.Web.Models;

public class DropDownSettingConfigurationModel : SettingConfigurationModel
{
    public DropDownSettingConfigurationModel(SettingDefinitionDataContract dataContract, Action<SettingEventArgs> stateChanged)
        : base(dataContract, stateChanged)
    {
        Value = dataContract.Value;
        ValidValues = dataContract.ValidValues;
        DefaultValue = dataContract.DefaultValue ?? ValidValues.FirstOrDefault();
    }

    public List<string> ValidValues { get; set; }

    public string Value { get; set; }

    public string? DefaultValue { get; set; }

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

    protected override void SetValue(dynamic value)
    {
        Value = Value;
    }

    internal override SettingConfigurationModel Clone(Action<SettingEventArgs> stateChanged)
    {
        var clone = new DropDownSettingConfigurationModel(_definitionDataContract, stateChanged)
        {
            IsDirty = true
        };

        return clone;
    }
}