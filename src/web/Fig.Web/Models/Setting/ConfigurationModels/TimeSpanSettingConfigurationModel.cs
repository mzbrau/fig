using Fig.Contracts.SettingDefinitions;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Web.Models.Setting.ConfigurationModels;

public class TimeSpanSettingConfigurationModel : SettingConfigurationModel<TimeSpan?>, ITimeSpanSettingModel
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
        var clone = new TimeSpanSettingConfigurationModel(DefinitionDataContract, parent, Presentation)
        {
            IsDirty = setDirty
        };

        return clone;
    }
}