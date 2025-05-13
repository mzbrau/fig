using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class LongSettingConfigurationModel : SettingConfigurationModel<long?>
{
    public LongSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent,
        SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
    }
    
    public long ConfirmUpdatedValue { get; set; }

    protected override bool IsUpdatedSecretValueValid()
    {
        return UpdatedValue == ConfirmUpdatedValue;
    }

    public override string IconKey => "looks_6";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new LongSettingConfigurationModel(DefinitionDataContract, parent, Presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}