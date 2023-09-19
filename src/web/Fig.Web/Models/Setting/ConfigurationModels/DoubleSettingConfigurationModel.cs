using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class DoubleSettingConfigurationModel : SettingConfigurationModel<double?>
{
    public DoubleSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent,
        bool isReadOnly)
        : base(dataContract, parent, isReadOnly)
    {
    }
    
    public double? ConfirmUpdatedValue { get; set; }

    protected override bool IsUpdatedSecretValueValid()
    {
        var updatedValue = UpdatedValue ?? 0;
        var confirmedValue = ConfirmUpdatedValue ?? 0;
        
        return Math.Abs(updatedValue - confirmedValue) < 0.000000000001;
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new DoubleSettingConfigurationModel(DefinitionDataContract, parent, isReadOnly)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}