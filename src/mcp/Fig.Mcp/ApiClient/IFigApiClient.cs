using Fig.Contracts.Authentication;
using Fig.Contracts.CheckPoint;
using Fig.Contracts.ClientRegistrationHistory;
using Fig.Contracts.Configuration;
using Fig.Contracts.CustomActions;
using Fig.Contracts.EventHistory;
using Fig.Contracts.ImportExport;
using Fig.Contracts.LookupTable;
using Fig.Contracts.Scheduling;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.Status;
using Fig.Contracts.WebHook;

namespace Fig.Mcp.ApiClient;

public interface IFigApiClient
{
    // ── Clients & Settings ───────────────────────────────────────────────

    Task<IList<SettingsClientDefinitionDataContract>> GetClientsAsync(
        CancellationToken ct = default);

    Task<ClientsDescriptionDataContract> GetClientDescriptionsAsync(
        CancellationToken ct = default);

    Task UpdateSettingsAsync(
        string clientName,
        IEnumerable<SettingDataContract> settings,
        string? changeMessage = null,
        CancellationToken ct = default);

    Task DeleteClientAsync(
        string clientName,
        CancellationToken ct = default);

    Task<ClientSecretChangeResponseDataContract> ChangeClientSecretAsync(
        string clientName,
        ClientSecretChangeRequestDataContract request,
        CancellationToken ct = default);

    // ── Events ───────────────────────────────────────────────────────────

    Task<EventLogCollectionDataContract> GetEventsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct = default);

    Task<EventLogCountDataContract> GetEventCountAsync(
        CancellationToken ct = default);

    Task<EventLogCollectionDataContract> GetClientTimelineAsync(
        string clientName,
        CancellationToken ct = default);

    // ── History ──────────────────────────────────────────────────────────

    Task<IEnumerable<SettingValueDataContract>> GetSettingHistoryAsync(
        string clientName,
        string settingName,
        string? instance = null,
        CancellationToken ct = default);

    Task<string> GetLastChangedAsync(
        CancellationToken ct = default);

    // ── Sessions / Statuses ──────────────────────────────────────────────

    Task<IEnumerable<ClientStatusDataContract>> GetRunSessionsAsync(
        CancellationToken ct = default);

    Task SetLiveReloadAsync(
        Guid runSessionId,
        bool enabled,
        CancellationToken ct = default);

    Task RestartSessionAsync(
        Guid runSessionId,
        CancellationToken ct = default);

    // ── Lookup Tables ────────────────────────────────────────────────────

    Task<IEnumerable<LookupTableDataContract>> GetLookupTablesAsync(
        CancellationToken ct = default);

    Task CreateLookupTableAsync(
        LookupTableDataContract table,
        CancellationToken ct = default);

    Task UpdateLookupTableAsync(
        Guid id,
        LookupTableDataContract table,
        CancellationToken ct = default);

    Task DeleteLookupTableAsync(
        Guid id,
        CancellationToken ct = default);

    // ── WebHooks ─────────────────────────────────────────────────────────

    Task<IEnumerable<WebHookDataContract>> GetWebHooksAsync(
        CancellationToken ct = default);

    Task CreateWebHookAsync(
        WebHookDataContract webHook,
        CancellationToken ct = default);

    Task UpdateWebHookAsync(
        Guid id,
        WebHookDataContract webHook,
        CancellationToken ct = default);

    Task DeleteWebHookAsync(
        Guid id,
        CancellationToken ct = default);

    Task<IEnumerable<WebHookClientDataContract>> GetWebHookClientsAsync(
        CancellationToken ct = default);

    Task CreateWebHookClientAsync(
        WebHookClientDataContract client,
        CancellationToken ct = default);

    Task UpdateWebHookClientAsync(
        Guid id,
        WebHookClientDataContract client,
        CancellationToken ct = default);

    Task DeleteWebHookClientAsync(
        Guid id,
        CancellationToken ct = default);

    Task TestWebHookClientAsync(
        Guid id,
        CancellationToken ct = default);

    // ── Time Machine ─────────────────────────────────────────────────────

    Task<CheckPointCollectionDataContract> GetCheckPointsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct = default);

    Task<string> GetCheckPointDataAsync(
        Guid dataId,
        CancellationToken ct = default);

    Task ApplyCheckPointAsync(
        Guid checkPointId,
        CancellationToken ct = default);

    Task UpdateCheckPointNoteAsync(
        Guid checkPointId,
        CheckPointUpdateDataContract update,
        CancellationToken ct = default);

    // ── Scheduling ───────────────────────────────────────────────────────

    Task<SchedulingChangesDataContract> GetDeferredChangesAsync(
        CancellationToken ct = default);

    Task RescheduleChangeAsync(
        Guid id,
        RescheduleDeferredChangeDataContract request,
        CancellationToken ct = default);

    Task DeleteScheduledChangeAsync(
        Guid id,
        CancellationToken ct = default);

    // ── Custom Actions ───────────────────────────────────────────────────

    Task<CustomActionExecutionResponseDataContract> ExecuteCustomActionAsync(
        string clientName,
        CustomActionExecutionRequestDataContract request,
        CancellationToken ct = default);

    Task<CustomActionExecutionStatusDataContract> GetCustomActionStatusAsync(
        Guid executionId,
        CancellationToken ct = default);

    Task<CustomActionExecutionHistoryDataContract> GetCustomActionHistoryAsync(
        string clientName,
        Guid customActionId,
        CancellationToken ct = default);

    // ── Users ────────────────────────────────────────────────────────────

    Task<IEnumerable<UserDataContract>> GetUsersAsync(
        CancellationToken ct = default);

    Task<UserDataContract> GetUserAsync(
        Guid id,
        CancellationToken ct = default);

    Task RegisterUserAsync(
        RegisterUserRequestDataContract request,
        CancellationToken ct = default);

    Task UpdateUserAsync(
        Guid id,
        UpdateUserRequestDataContract user,
        CancellationToken ct = default);

    Task DeleteUserAsync(
        Guid id,
        CancellationToken ct = default);

    // ── Data Import / Export ─────────────────────────────────────────────

    Task<FigDataExportDataContract> ExportDataAsync(
        CancellationToken ct = default);

    Task<ImportResultDataContract> ImportDataAsync(
        FigDataExportDataContract data,
        CancellationToken ct = default);

    Task<FigValueOnlyDataExportDataContract> ExportValueOnlyDataAsync(
        CancellationToken ct = default);

    Task<ImportResultDataContract> ImportValueOnlyDataAsync(
        FigValueOnlyDataExportDataContract data,
        CancellationToken ct = default);

    Task DeleteDeferredImportsAsync(
        CancellationToken ct = default);

    // ── Configuration & Status ───────────────────────────────────────────

    Task<FigConfigurationDataContract> GetConfigurationAsync(
        CancellationToken ct = default);

    Task UpdateConfigurationAsync(
        FigConfigurationDataContract configuration,
        CancellationToken ct = default);

    Task<IEnumerable<ApiStatusDataContract>> GetApiStatusAsync(
        CancellationToken ct = default);

    Task<ApiVersionDataContract> GetApiVersionAsync(
        CancellationToken ct = default);

    Task<ClientRegistrationHistoryCollectionDataContract> GetClientRegistrationHistoryAsync(
        CancellationToken ct = default);

    Task<IEnumerable<DeferredImportClientDataContract>> GetDeferredImportsAsync(
        CancellationToken ct = default);
}
