using Fig.Api.Utils;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.WebHooks;

public record SettingValueChangedWebHookData(
    List<ChangedSetting> Changes,
    SettingClientBusinessEntity Client,
    string? Username,
    string ChangeMessage)
{
    public SettingValueChangedWebHookData() 
        : this(new(), null!, null, string.Empty)
    {
    }
};
