using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public interface ISettingChangeRecorder
{
    void RecordSettingChanges(List<ChangedSetting> changes, string? changeMessage,
        DateTime timeOfUpdate,
        SettingClientBusinessEntity client,
        string? userName);
}