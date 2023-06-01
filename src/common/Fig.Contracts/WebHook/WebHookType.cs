namespace Fig.Contracts.WebHook;

public enum WebHookType
{
    ClientStatusChanged,
    SettingValueChanged,
    MemoryLeakDetected,
    NewClientRegistration,
    UpdatedClientRegistration,
    MinRunSessions
}