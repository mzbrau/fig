using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public interface ISettingChangeRecorder
{
    void RecordSettingChanges(List<ChangedSetting> changes, string? changeMessage,
        SettingClientBusinessEntity client,
        string? instance, string? userName);
}