using System.Text;
using Fig.Common.NetStandard.Json;
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
using Newtonsoft.Json;

namespace Fig.Mcp.ApiClient;

public class FigApiClient : IFigApiClient
{
    private readonly HttpClient _httpClient;

    public FigApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ── Clients & Settings ───────────────────────────────────────────────

    public async Task<IList<SettingsClientDefinitionDataContract>> GetClientsAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<IList<SettingsClientDefinitionDataContract>>("clients", ct);
    }

    public async Task<ClientsDescriptionDataContract> GetClientDescriptionsAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<ClientsDescriptionDataContract>("clients/descriptions", ct);
    }

    public async Task UpdateSettingsAsync(
        string clientName,
        IEnumerable<SettingDataContract> settings,
        string? changeMessage = null,
        CancellationToken ct = default)
    {
        var url = $"clients/{Uri.EscapeDataString(clientName)}/settings";
        var body = new SettingValueUpdatesDataContract(settings, changeMessage ?? string.Empty);
        await PutAsync(url, body, ct);
    }

    public async Task DeleteClientAsync(string clientName, CancellationToken ct = default)
    {
        await DeleteAsync($"clients/{Uri.EscapeDataString(clientName)}", ct);
    }

    public async Task<ClientSecretChangeResponseDataContract> ChangeClientSecretAsync(
        string clientName,
        ClientSecretChangeRequestDataContract request,
        CancellationToken ct = default)
    {
        return await PutAsync<ClientSecretChangeResponseDataContract>(
            $"clients/{Uri.EscapeDataString(clientName)}/secret", request, ct);
    }

    // ── Events ───────────────────────────────────────────────────────────

    public async Task<EventLogCollectionDataContract> GetEventsAsync(
        DateTime startTime, DateTime endTime, CancellationToken ct = default)
    {
        var url = $"events?startTime={Uri.EscapeDataString(startTime.ToString("O"))}&endTime={Uri.EscapeDataString(endTime.ToString("O"))}";
        return await GetAsync<EventLogCollectionDataContract>(url, ct);
    }

    public async Task<EventLogCountDataContract> GetEventCountAsync(CancellationToken ct = default)
    {
        return await GetAsync<EventLogCountDataContract>("events/Count", ct);
    }

    public async Task<EventLogCollectionDataContract> GetClientTimelineAsync(
        string clientName, CancellationToken ct = default)
    {
        return await GetAsync<EventLogCollectionDataContract>(
            $"events/client/{Uri.EscapeDataString(clientName)}/timeline", ct);
    }

    // ── History ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<SettingValueDataContract>> GetSettingHistoryAsync(
        string clientName, string settingName, string? instance = null,
        CancellationToken ct = default)
    {
        var url = $"clients/{Uri.EscapeDataString(clientName)}/settings/{Uri.EscapeDataString(settingName)}/history";
        if (!string.IsNullOrEmpty(instance))
            url += $"?instance={Uri.EscapeDataString(instance)}";

        return await GetAsync<IEnumerable<SettingValueDataContract>>(url, ct);
    }

    public async Task<string> GetLastChangedAsync(CancellationToken ct = default)
    {
        return await GetRawAsync("clients/settings/lastchanged", ct);
    }

    // ── Sessions / Statuses ──────────────────────────────────────────────

    public async Task<IEnumerable<ClientStatusDataContract>> GetRunSessionsAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<IEnumerable<ClientStatusDataContract>>("statuses", ct);
    }

    public async Task SetLiveReloadAsync(Guid runSessionId, bool enabled,
        CancellationToken ct = default)
    {
        var url = $"statuses/{runSessionId}/liveReload?liveReload={enabled.ToString().ToLowerInvariant()}";
        await PutAsync(url, ct);
    }

    public async Task RestartSessionAsync(Guid runSessionId, CancellationToken ct = default)
    {
        await PutAsync($"statuses/{runSessionId}/restart", ct);
    }

    // ── Lookup Tables ────────────────────────────────────────────────────

    public async Task<IEnumerable<LookupTableDataContract>> GetLookupTablesAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<IEnumerable<LookupTableDataContract>>("lookuptables", ct);
    }

    public async Task CreateLookupTableAsync(LookupTableDataContract table,
        CancellationToken ct = default)
    {
        await PostAsync("lookuptables", table, ct);
    }

    public async Task UpdateLookupTableAsync(Guid id, LookupTableDataContract table,
        CancellationToken ct = default)
    {
        await PutAsync($"lookuptables/{id}", table, ct);
    }

    public async Task DeleteLookupTableAsync(Guid id, CancellationToken ct = default)
    {
        await DeleteAsync($"lookuptables/{id}", ct);
    }

    // ── WebHooks ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<WebHookDataContract>> GetWebHooksAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<IEnumerable<WebHookDataContract>>("webhooks", ct);
    }

    public async Task CreateWebHookAsync(WebHookDataContract webHook,
        CancellationToken ct = default)
    {
        await PostAsync("webhooks", webHook, ct);
    }

    public async Task UpdateWebHookAsync(Guid id, WebHookDataContract webHook,
        CancellationToken ct = default)
    {
        await PutAsync($"webhooks/{id}", webHook, ct);
    }

    public async Task DeleteWebHookAsync(Guid id, CancellationToken ct = default)
    {
        await DeleteAsync($"webhooks/{id}", ct);
    }

    public async Task<IEnumerable<WebHookClientDataContract>> GetWebHookClientsAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<IEnumerable<WebHookClientDataContract>>("webhookclient", ct);
    }

    public async Task CreateWebHookClientAsync(WebHookClientDataContract client,
        CancellationToken ct = default)
    {
        await PostAsync("webhookclient", client, ct);
    }

    public async Task UpdateWebHookClientAsync(Guid id, WebHookClientDataContract client,
        CancellationToken ct = default)
    {
        await PutAsync($"webhookclient/{id}", client, ct);
    }

    public async Task DeleteWebHookClientAsync(Guid id, CancellationToken ct = default)
    {
        await DeleteAsync($"webhookclient/{id}", ct);
    }

    public async Task TestWebHookClientAsync(Guid id, CancellationToken ct = default)
    {
        await PutAsync($"webhookclient/{id}/test", ct);
    }

    // ── Time Machine ─────────────────────────────────────────────────────

    public async Task<CheckPointCollectionDataContract> GetCheckPointsAsync(
        DateTime startTime, DateTime endTime, CancellationToken ct = default)
    {
        var url = $"timemachine?startTime={Uri.EscapeDataString(startTime.ToString("O"))}&endTime={Uri.EscapeDataString(endTime.ToString("O"))}";
        return await GetAsync<CheckPointCollectionDataContract>(url, ct);
    }

    public async Task<string> GetCheckPointDataAsync(Guid dataId,
        CancellationToken ct = default)
    {
        return await GetRawAsync($"timemachine/data?dataId={dataId}", ct);
    }

    public async Task ApplyCheckPointAsync(Guid checkPointId, CancellationToken ct = default)
    {
        await PutAsync($"timemachine/{checkPointId}", ct);
    }

    public async Task UpdateCheckPointNoteAsync(Guid checkPointId,
        CheckPointUpdateDataContract update, CancellationToken ct = default)
    {
        await PutAsync($"timemachine/{checkPointId}/note", update, ct);
    }

    // ── Scheduling ───────────────────────────────────────────────────────

    public async Task<SchedulingChangesDataContract> GetDeferredChangesAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<SchedulingChangesDataContract>("scheduling", ct);
    }

    public async Task RescheduleChangeAsync(Guid id,
        RescheduleDeferredChangeDataContract request, CancellationToken ct = default)
    {
        await PutAsync($"scheduling/{id}", request, ct);
    }

    public async Task DeleteScheduledChangeAsync(Guid id, CancellationToken ct = default)
    {
        await DeleteAsync($"scheduling/{id}", ct);
    }

    // ── Custom Actions ───────────────────────────────────────────────────

    public async Task<CustomActionExecutionResponseDataContract> ExecuteCustomActionAsync(
        string clientName, CustomActionExecutionRequestDataContract request,
        CancellationToken ct = default)
    {
        return await PutAsync<CustomActionExecutionResponseDataContract>(
            $"customactions/execute/{Uri.EscapeDataString(clientName)}", request, ct);
    }

    public async Task<CustomActionExecutionStatusDataContract> GetCustomActionStatusAsync(
        Guid executionId, CancellationToken ct = default)
    {
        return await GetAsync<CustomActionExecutionStatusDataContract>(
            $"customactions/status/{executionId}", ct);
    }

    public async Task<CustomActionExecutionHistoryDataContract> GetCustomActionHistoryAsync(
        string clientName, Guid customActionId, CancellationToken ct = default)
    {
        return await GetAsync<CustomActionExecutionHistoryDataContract>(
            $"customactions/history/{Uri.EscapeDataString(clientName)}/{customActionId}", ct);
    }

    // ── Users ────────────────────────────────────────────────────────────

    public async Task<IEnumerable<UserDataContract>> GetUsersAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<IEnumerable<UserDataContract>>("users", ct);
    }

    public async Task<UserDataContract> GetUserAsync(Guid id, CancellationToken ct = default)
    {
        return await GetAsync<UserDataContract>($"users/{id}", ct);
    }

    public async Task RegisterUserAsync(RegisterUserRequestDataContract request,
        CancellationToken ct = default)
    {
        await PostAsync("users/register", request, ct);
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserRequestDataContract user,
        CancellationToken ct = default)
    {
        await PutAsync($"users/{id}", user, ct);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        await DeleteAsync($"users/{id}", ct);
    }

    // ── Data Import / Export ─────────────────────────────────────────────

    public async Task<FigDataExportDataContract> ExportDataAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<FigDataExportDataContract>("data", ct);
    }

    public async Task<ImportResultDataContract> ImportDataAsync(
        FigDataExportDataContract data, CancellationToken ct = default)
    {
        return await PutAsync<ImportResultDataContract>("data", data, ct);
    }

    public async Task<FigValueOnlyDataExportDataContract> ExportValueOnlyDataAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<FigValueOnlyDataExportDataContract>("valueonlydata", ct);
    }

    public async Task<ImportResultDataContract> ImportValueOnlyDataAsync(
        FigValueOnlyDataExportDataContract data, CancellationToken ct = default)
    {
        return await PutAsync<ImportResultDataContract>("valueonlydata", data, ct);
    }

    public async Task DeleteDeferredImportsAsync(CancellationToken ct = default)
    {
        await DeleteAsync("data/deferredimports", ct);
    }

    // ── Configuration & Status ───────────────────────────────────────────

    public async Task<FigConfigurationDataContract> GetConfigurationAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<FigConfigurationDataContract>("configuration", ct);
    }

    public async Task UpdateConfigurationAsync(FigConfigurationDataContract configuration,
        CancellationToken ct = default)
    {
        await PutAsync("configuration", configuration, ct);
    }

    public async Task<IEnumerable<ApiStatusDataContract>> GetApiStatusAsync(CancellationToken ct = default)
    {
        return await GetAsync<IEnumerable<ApiStatusDataContract>>("apistatus", ct);
    }

    public async Task<ApiVersionDataContract> GetApiVersionAsync(CancellationToken ct = default)
    {
        return await GetAsync<ApiVersionDataContract>("apiversion", ct);
    }

    public async Task<ClientRegistrationHistoryCollectionDataContract>
        GetClientRegistrationHistoryAsync(CancellationToken ct = default)
    {
        return await GetAsync<ClientRegistrationHistoryCollectionDataContract>(
            "clientregistrationhistory", ct);
    }

    public async Task<IEnumerable<DeferredImportClientDataContract>> GetDeferredImportsAsync(
        CancellationToken ct = default)
    {
        return await GetAsync<IEnumerable<DeferredImportClientDataContract>>(
            "deferredimport", ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, ct);
        await EnsureSuccessAsync(response);
        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonConvert.DeserializeObject<T>(body, JsonSettings.FigDefault)!;
    }

    private async Task<string> GetRawAsync(string url, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task PostAsync(string url, object body, CancellationToken ct)
    {
        using var content = CreateJsonContent(body);
        using var response = await _httpClient.PostAsync(url, content, ct);
        await EnsureSuccessAsync(response);
    }

    private async Task PutAsync(string url, object body, CancellationToken ct)
    {
        using var content = CreateJsonContent(body);
        using var response = await _httpClient.PutAsync(url, content, ct);
        await EnsureSuccessAsync(response);
    }

    private async Task PutAsync(string url, CancellationToken ct)
    {
        using var response = await _httpClient.PutAsync(url, null, ct);
        await EnsureSuccessAsync(response);
    }

    private async Task<T> PutAsync<T>(string url, object body, CancellationToken ct)
    {
        using var content = CreateJsonContent(body);
        using var response = await _httpClient.PutAsync(url, content, ct);
        await EnsureSuccessAsync(response);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        return JsonConvert.DeserializeObject<T>(responseBody, JsonSettings.FigDefault)!;
    }

    private async Task DeleteAsync(string url, CancellationToken ct)
    {
        using var response = await _httpClient.DeleteAsync(url, ct);
        await EnsureSuccessAsync(response);
    }

    private static StringContent CreateJsonContent(object body)
    {
        var json = JsonConvert.SerializeObject(body, JsonSettings.FigDefault);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Fig API request failed with status {(int)response.StatusCode} ({response.StatusCode}): {body}");
        }
    }
}
