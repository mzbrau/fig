using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class TimeSpanSettingConfigurationModel : SettingConfigurationModel<TimeSpan?>
{
    public TimeSpanSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent,
        SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
    }

    public override string IconKey => "timer";

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        var clone = new TimeSpanSettingConfigurationModel(DefinitionDataContract, parent, _presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}