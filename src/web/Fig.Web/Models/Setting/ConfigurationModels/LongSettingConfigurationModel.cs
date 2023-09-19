using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class LongSettingConfigurationModel : SettingConfigurationModel<long?>
{
    public LongSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent,
        bool isReadOnly)
        : base(dataContract, parent, isReadOnly)
    {
    }
    
    public long ConfirmUpdatedValue { get; set; }

    protected override bool IsUpdatedSecretValueValid()
    {
        return UpdatedValue == ConfirmUpdatedValue;
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new LongSettingConfigurationModel(DefinitionDataContract, parent, isReadOnly)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}