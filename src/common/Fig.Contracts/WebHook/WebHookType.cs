namespace Fig.Contracts.WebHook;

public enum WebHookType
{
    ClientStatusChanged,
    SettingValueChanged,
    NewClientRegistration,
    UpdatedClientRegistration,
    MinRunSessions,
    ConfigurationError,
    HealthStatusChanged
}