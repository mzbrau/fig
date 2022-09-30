using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public interface ISettingChangeRecorder
{
    void RecordSettingChanges(List<ChangedSetting> changes, SettingClientBusinessEntity client,
        string? instance, string? userName);
}