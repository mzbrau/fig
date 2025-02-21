using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public interface ISettingChangeRecorder
{
    Task RecordSettingChanges(List<ChangedSetting> changes, string? changeMessage,
        DateTime timeOfUpdate,
        SettingClientBusinessEntity client,
        string? userName);
}