namespace Fig.Common.Constants;

public static class EventMessage
{
    public const string SettingValueUpdated = "Setting value updated";
    public const string InitialRegistration = "Initial Registration";
    public const string RegistrationNoChange = "Registration - No Change";
    public const string RegistrationWithChange = "Registration - Definition Changed";
    public const string ClientDeleted = "Setting Client Deleted";
    public const string ClientInstanceCreated = "Client instance created";
    public const string SettingsRead = "Settings Read";
    public const string Login = "Login";
    public const string LoginFailed = "Login Failed";
    public const string UserCreated = "User created";
    public const string PasswordUpdated = "Password Updated";
    public const string UserUpdated = "User Updated";
    public const string UserDeleted = "User deleted";
    public const string NewSession = "New Run Session";
    public const string ExpiredSession = "Run Session Expired";
    public const string UnknownHostname = "Unknown Hostname";
    public const string UnknownIp = "Unknown IP Address";
    public const string DataExported = "Data Exported";
    public const string DataImported = "Data Imported";
    public const string DataImportFailed = "Data Import Failed";
    public const string DeferredImportRegistered = "Deferred Import Registered";
    public const string DeferredImportApplied = "Deferred Import Applied";
    public const string DataImportStarted = "Data Import Started";
    public const string ClientImported = "Client Imported";
    public const string ConfigurationChanged = "Fig Configuration Changed";
    public const string WebHookSent = "WebHook Sent";
    public const string ClientSecretChanged = "Client Secret Changed";
    public const string LiveReloadChanged = "Live Reload Changed";
    public const string RestartRequested = "Restart Requested";
    public const string CheckPointCreated = "CheckPoint Created";
    public const string CheckPointApplied = "CheckPoint Applied";
    public const string NoteAddedToCheckPoint = "Note added to CheckPoint";
    public const string ExternallyManagedSettingUpdatedByUser = "Externally Managed Setting Updated By User";
    public const string ChangesScheduled = "Changes Scheduled";
    public const string ScheduledChangesDeleted = "Scheduled Changes Deleted";
    public const string HealthStatusChanged = "Client Health Status Changed";
    public const string CustomActionAdded = "Custom Action Added";
    public const string CustomActionsRemoved = "Custom Actions Removed";
    public const string CustomActionUpdated = "Custom Action Updated";
    public const string CustomActionExecutionRequested = "Custom Action Execution Requested";
    public const string CustomActionExecutionCompleted = "Custom Action Execution Completed";
    public const string InvalidClientSecretAttempt = "Invalid Client Secret Attempt";

    public static readonly List<string> UnrestrictedEvents =
    [
        SettingValueUpdated,
        InitialRegistration,
        RegistrationNoChange,
        RegistrationWithChange,
        ClientDeleted,
        ClientInstanceCreated,
        SettingsRead,
        NewSession,
        ExpiredSession,
        UnknownHostname,
        UnknownIp,
        DataExported,
        DataImported,
        DataImportFailed,
        DataImportStarted,
        ClientImported,
        ConfigurationChanged,
        WebHookSent,
        ClientSecretChanged,
        HealthStatusChanged
    ];
}