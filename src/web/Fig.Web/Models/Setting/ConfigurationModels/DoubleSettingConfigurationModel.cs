using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class DoubleSettingConfigurationModel : SettingConfigurationModel<double?>
{
    public DoubleSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent,
        SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
    }
    
    public double? ConfirmUpdatedValue { get; set; }

    protected override bool IsUpdatedSecretValueValid()
    {
        var updatedValue = UpdatedValue ?? 0;
        var confirmedValue = ConfirmUpdatedValue ?? 0;
        
        return Math.Abs(updatedValue - confirmedValue) < 0.000000000001;
    }

    public override string IconKey => "decimal_increase";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new DoubleSettingConfigurationModel(DefinitionDataContract, parent, Presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}