using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class DateTimeSettingConfigurationModel : SettingConfigurationModel<DateTime?>
{
    public DateTimeSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        return new DateTimeSettingConfigurationModel(DefinitionDataContract, parent)
        {
            IsDirty = setDirty
        };
    }
}