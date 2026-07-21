using Fig.Web.Models.Setting;

namespace Fig.Web.Facades;

public class SaveClientsBatchResult
{
    public Dictionary<SettingClientConfigurationModel, List<string>> SuccessfulChanges { get; } = new();

    public List<string> Failures { get; } = new();
}
