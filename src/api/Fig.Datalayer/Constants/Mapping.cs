namespace Fig.Datalayer.Constants;

public static class Mapping
{
    public static readonly int NVarCharMax = 10000; // Anything over 4000 will be mapped to nvarchar(max)

    public static readonly string ApiStatusTable = "api_status";
    public static readonly string RunSessionsTable = "run_sessions";
    public static readonly string SettingClientsTable = "setting_clients";
    public static readonly string DeferredClientImportsTable = "deferred_client_imports";
    public static readonly string EventLogsTable = "event_logs";
    public static readonly string ConfigurationTable = "configuration";
    public static readonly string LookupTablesTable = "lookup_tables";
    public static readonly string SettingsTable = "settings";
    public static readonly string SettingVerificationsTable = "setting_verifications";
    public static readonly string SettingValueHistoryTable = "setting_value_history";
    public static readonly string UsersTable = "users";
    public static readonly string VerificationResultHistoryTable = "verification_result_history";
    public static readonly string WebHookClientTable = "web_hook_clients";
    public static readonly string WebHooksTable = "web_hooks";
    public static readonly string SettingChangeTable = "setting_change";
    public static readonly string CheckPointTable = "check_points";
    public static readonly string CheckPointDataTable = "check_point_data";
    public static readonly string DeferredChangeTable = "deferred_change";
    public static readonly string CheckPointTriggerTable = "check_point_trigger";
    public static readonly string CustomActionsTable = "custom_actions";
    public static readonly string CustomActionExecutionsTable = "custom_action_executions";
    public static readonly string CustomActionExecutionResultsTable = "custom_action_execution_results";
}