namespace Fig.Common.NetStandard.WebHook;

public enum WebHookType
{
    ClientStatusChanged,
    SettingChanged,
    MemoryLeakDetected,
    NewClientRegistration,
    UpdatedClientRegistration,
    BelowMinRunSessions
}