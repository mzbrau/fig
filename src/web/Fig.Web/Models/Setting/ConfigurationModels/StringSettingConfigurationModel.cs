using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class StringSettingConfigurationModel : SettingConfigurationModel<string?>
{
    public StringSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
    }

    public string ConfirmUpdatedValue { get; set; } = string.Empty;

    public bool CanClearSecretValue => IsSecret && !string.IsNullOrEmpty(Value);

    protected override bool IsUpdatedSecretValueValid()
    {
        return !string.IsNullOrWhiteSpace(UpdatedValue) &&
               UpdatedValue == ConfirmUpdatedValue;
    }

    public void ClearSecretValue()
    {
        if (IsSecret)
        {
            Value = null;
            IsDirty = true;
        }
    }

    public override string IconKey => "text_fields";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new StringSettingConfigurationModel(DefinitionDataContract, parent, Presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}