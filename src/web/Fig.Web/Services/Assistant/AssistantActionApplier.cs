using System.Globalization;
using System.Text;
using Fig.Contracts.Assistant;
using Fig.Contracts.SettingGroups;
using Fig.Web.Facades;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen;

namespace Fig.Web.Services.Assistant;

public sealed class AssistantActionApplier : IAssistantActionApplier
{
    private readonly ISettingClientFacade _settingClientFacade;
    private readonly IGroupsFacade _groupsFacade;
    private readonly ILookupTablesFacade _lookupTablesFacade;
    private readonly IAssistantUiActionQueue _uiActionQueue;
    private readonly NavigationManager _navigationManager;
    private readonly IReportsFacade _reportsFacade;
    private readonly IJSRuntime _jsRuntime;
    private readonly NotificationService _notificationService;
    private readonly INotificationFactory _notificationFactory;

    public AssistantActionApplier(
        ISettingClientFacade settingClientFacade,
        IGroupsFacade groupsFacade,
        ILookupTablesFacade lookupTablesFacade,
        IAssistantUiActionQueue uiActionQueue,
        NavigationManager navigationManager,
        IReportsFacade reportsFacade,
        IJSRuntime jsRuntime,
        NotificationService notificationService,
        INotificationFactory notificationFactory)
    {
        _settingClientFacade = settingClientFacade;
        _groupsFacade = groupsFacade;
        _lookupTablesFacade = lookupTablesFacade;
        _uiActionQueue = uiActionQueue;
        _navigationManager = navigationManager;
        _reportsFacade = reportsFacade;
        _jsRuntime = jsRuntime;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
    }

    public async Task ApplyAsync(
        IReadOnlyCollection<AssistantProposedActionDataContract> actions,
        CancellationToken cancellationToken = default)
    {
        var draftCount = 0;
        var needsSettingsPage = false;
        var needsGroupsPage = false;
        var needsLookupTablesPage = false;
        foreach (var action in actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var kind = await Apply(action);
                if (kind is AppliedKind.Draft or AppliedKind.DraftWithUi or AppliedKind.DraftWithGroups
                    or AppliedKind.DraftWithLookupTables)
                    draftCount++;
                if (kind is AppliedKind.UiNavigation or AppliedKind.DraftWithUi)
                    needsSettingsPage = true;
                if (kind is AppliedKind.DraftWithGroups)
                    needsGroupsPage = true;
                if (kind is AppliedKind.DraftWithLookupTables)
                    needsLookupTablesPage = true;
            }
            catch (Exception ex)
            {
                _notificationService.Notify(_notificationFactory.Failure(
                    "Assistant action failed", ex.Message));
            }
        }

        if (needsSettingsPage)
            EnsureOnSettingsPage();
        if (needsGroupsPage)
            EnsureOnPage("groups");
        if (needsLookupTablesPage)
            EnsureOnPage("lookuptables");

        if (draftCount > 0)
        {
            _notificationService.Notify(_notificationFactory.Success(
                "Assistant changes ready",
                $"{draftCount} proposed change(s) were added as unsaved drafts. Review and save them on the relevant page."));
        }
    }

    private async Task<AppliedKind> Apply(AssistantProposedActionDataContract action)
    {
        switch (action.Type)
        {
            case AssistantProposedActionTypes.UpdateSetting:
            {
                var clientName = Required(action.ClientName, "clientName");
                var settingName = Required(action.SettingName, "settingName");
                _settingClientFacade.ApplyPendingValueFromCompare(
                    clientName,
                    action.Instance,
                    settingName,
                    SerializeSettingValue(action.Value));
                _uiActionQueue.EnqueueHighlight(clientName, settingName, action.Instance);
                return AppliedKind.DraftWithUi;
            }

            case AssistantProposedActionTypes.CreateGroup:
                _groupsFacade.AddDraftGroup(
                    Required(action.GroupName, "groupName"),
                    action.Description,
                    ConvertGroupSettings(action.Data));
                return AppliedKind.DraftWithGroups;

            case AssistantProposedActionTypes.CreateLookupTable:
                _lookupTablesFacade.CreateDraft(
                    Required(action.LookupTableName, "lookupTableName"),
                    ConvertLookupDataToCsv(action.Data));
                return AppliedKind.DraftWithLookupTables;

            case AssistantProposedActionTypes.CreateInstance:
                await _settingClientFacade.CreatePendingInstance(
                    Required(action.ClientName, "clientName"),
                    Required(action.Instance, "instance"));
                return AppliedKind.DraftWithUi;

            case AssistantProposedActionTypes.SearchSettings:
                _uiActionQueue.EnqueueSearch(Required(action.SearchQuery, "searchQuery"));
                return AppliedKind.UiNavigation;

            case AssistantProposedActionTypes.HighlightSetting:
                _uiActionQueue.EnqueueHighlight(
                    Required(action.ClientName, "clientName"),
                    Required(action.SettingName, "settingName"),
                    action.Instance);
                return AppliedKind.UiNavigation;

            case AssistantProposedActionTypes.GenerateReport:
                await GenerateAndOpenReport(action);
                return AppliedKind.Immediate;

            default:
                throw new InvalidOperationException($"Unsupported assistant action '{action.Type}'.");
        }
    }

    private async Task GenerateAndOpenReport(AssistantProposedActionDataContract action)
    {
        var reportId = Required(action.ReportId, "reportId");
        var parameters = NormalizeParameters(action.Parameters);
        var html = await _reportsFacade.GenerateReport(reportId, parameters);
        if (string.IsNullOrWhiteSpace(html))
        {
            _notificationService.Notify(_notificationFactory.Failure(
                "Report Failed",
                "No HTML was returned from the API."));
            return;
        }

        var opened = await _jsRuntime.InvokeAsync<bool>("openHtmlInNewTab", html);
        if (!opened)
        {
            _notificationService.Notify(_notificationFactory.Failure(
                "Popup Blocked",
                "Allow pop-ups for Fig to open the generated report."));
            return;
        }

        _notificationService.Notify(_notificationFactory.Success(
            "Report Generated",
            "Opened in a new tab. Use Print to save as PDF."));
    }

    private static Dictionary<string, object?> NormalizeParameters(Dictionary<string, object?>? parameters)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (parameters is null)
            return result;

        foreach (var (key, value) in parameters)
        {
            if (value is JToken token)
                result[key] = token.Type == JTokenType.Null ? null : token.ToObject<object>();
            else
                result[key] = value;
        }

        return result;
    }

    private void EnsureOnSettingsPage()
    {
        var relative = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);
        if (!string.IsNullOrEmpty(relative) &&
            !relative.Equals("settings", StringComparison.OrdinalIgnoreCase) &&
            !relative.StartsWith("settings?", StringComparison.OrdinalIgnoreCase) &&
            !relative.StartsWith("settings/", StringComparison.OrdinalIgnoreCase))
        {
            _navigationManager.NavigateTo("/");
        }
    }

    private void EnsureOnPage(string pageSegment)
    {
        var relative = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);
        if (relative.Equals(pageSegment, StringComparison.OrdinalIgnoreCase) ||
            relative.StartsWith($"{pageSegment}?", StringComparison.OrdinalIgnoreCase) ||
            relative.StartsWith($"{pageSegment}/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _navigationManager.NavigateTo($"/{pageSegment}");
    }

    private static string? SerializeSettingValue(object? value)
    {
        if (value is null)
            return null;

        var token = value as JToken ?? JToken.FromObject(value);
        return token.Type == JTokenType.String
            ? token.Value<string>()
            : token.ToString(Formatting.None);
    }

    private static List<GroupedSettingDataContract>? ConvertGroupSettings(object? data)
    {
        if (data is null)
            return null;

        var token = data as JToken ?? JToken.FromObject(data);
        if (token is JObject obj && obj["groupedSettings"] is { } groupedSettings)
            token = groupedSettings;

        return token.ToObject<List<GroupedSettingDataContract>>();
    }

    private static string? ConvertLookupDataToCsv(object? data)
    {
        if (data is null)
            return null;
        if (data is string text)
            return text;

        var token = data as JToken ?? JToken.FromObject(data);
        if (token.Type == JTokenType.String)
            return token.Value<string>();
        if (token is JObject obj && obj["rows"] is { } rows)
            token = rows;
        if (token is not JArray array)
            return CsvCell(token);

        var lines = new List<string>();
        foreach (var row in array)
        {
            if (row is JArray cells)
            {
                lines.Add(string.Join(",", cells.Select(CsvCell)));
            }
            else if (row is JObject rowObject)
            {
                var key = rowObject["key"] ?? rowObject.Properties().FirstOrDefault()?.Value;
                var alias = rowObject["alias"] ?? rowObject.Properties().Skip(1).FirstOrDefault()?.Value;
                lines.Add(alias is null
                    ? CsvCell(key)
                    : $"{CsvCell(key)},{CsvCell(alias)}");
            }
            else
            {
                lines.Add(CsvCell(row));
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string CsvCell(JToken? token)
    {
        var value = token?.Type == JTokenType.String
            ? token.Value<string>() ?? string.Empty
            : Convert.ToString((token as JValue)?.Value, CultureInfo.InvariantCulture)
              ?? token?.ToString(Formatting.None)
              ?? string.Empty;
        return value.IndexOfAny([',', '"', '\r', '\n']) < 0
            ? value
            : $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string Required(string? value, string name) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Assistant action is missing {name}.");

    private enum AppliedKind
    {
        Draft,
        DraftWithUi,
        DraftWithGroups,
        DraftWithLookupTables,
        UiNavigation,
        Immediate
    }
}
