using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class LongSettingConfigurationModel : SettingConfigurationModel<long?>
{
    public LongSettingConfigurationModel(SettingDefinitionDataContract dataContract, SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
    }
    
    public long ConfirmUpdatedValue { get; set; }

    protected override bool IsUpdatedSecretValueValid()
    {
        return UpdatedValue == ConfirmUpdatedValue;
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        var clone = new LongSettingConfigurationModel(DefinitionDataContract, parent)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}