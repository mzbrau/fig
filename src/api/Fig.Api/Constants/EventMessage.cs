namespace Fig.Api.Constants;

public static class EventMessage
{
    public const string SettingValueUpdated = "Setting value updated";
    public const string InitialRegistration = "Initial Registration ";
    public const string RegistrationNoChange = "Registration - No Change";
    public const string RegistrationWithChange = "Registration - Definition Changed";
    public const string ClientDeleted = "Setting Client Deleted";
    public const string ClientInstanceCreated = "Client instance created";
    public const string SettingVerificationRun = "Setting verification run";
    public const string SettingsRead = "Settings Read";
    public const string Login = "Login";
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
    public const string DeferredImportRegistered = "Deferred Import Registered";
    public const string DeferredImportApplied = "Deferred Import Applied";
    public const string DataImportStarted = "Data Import Started";
    public const string ClientImported = "Client Imported";
    public const string ConfigurationChanged = "Fig Configuration Changed";
    public const string HasConfigurationError = "Has Configuration Error";
    public const string ConfigurationErrorCleared = "Configuration Error Cleared";
    public const string ConfigurationError = "Configuration Error";

    public static List<string> UnrestrictedEvents => new()
    {
        SettingValueUpdated,
        InitialRegistration,
        RegistrationNoChange,
        RegistrationWithChange,
        ClientDeleted,
        ClientInstanceCreated,
        SettingVerificationRun,
        SettingsRead,
        NewSession,
        ExpiredSession,
        UnknownHostname,
        UnknownIp,
        DataExported,
        DataImported,
        DataImportStarted,
        ClientImported,
        ConfigurationChanged,
        HasConfigurationError,
        ConfigurationErrorCleared,
        ConfigurationError
    };

    
}