using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class DateTimeSettingConfigurationModel : SettingConfigurationModel<DateTime?>
{
    public DateTimeSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
    }

    public override string IconKey => "event";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        return new DateTimeSettingConfigurationModel(DefinitionDataContract, parent, Presentation)
        {
            IsDirty = setDirty
        };
    }
}