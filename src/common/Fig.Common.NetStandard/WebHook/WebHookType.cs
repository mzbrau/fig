namespace Fig.Common.NetStandard.WebHook;

public enum WebHookType
{
    ClientStatusChanged,
    SettingValueChanged,
    MemoryLeakDetected,
    NewClientRegistration,
    UpdatedClientRegistration,
    MinRunSessions
}