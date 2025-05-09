using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class IntSettingConfigurationModel : SettingConfigurationModel<int?>
{
    public IntSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
    }

    public int? ConfirmUpdatedValue { get; set; }

    protected override bool IsUpdatedSecretValueValid()
    {
        return UpdatedValue == ConfirmUpdatedValue;
    }

    public override string IconKey => "looks_one";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new IntSettingConfigurationModel(DefinitionDataContract, parent, _presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}