using Fig.Contracts.Reports;
using Fig.Web.Attributes;
using Fig.Web.Facades;
using Fig.Web.Models.Reports;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace Fig.Web.Pages;

public partial class Reports
{
    private bool _isLoading = true;
    private bool _isGenerating;
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _usernames = new();
    private List<string> _groupNames = new();
    private List<ClientOption> _clientOptions = new();
    private List<GroupOption> _groupOptions = new();
    private List<string> _settingNames = new();

    [Inject] private IReportsFacade ReportsFacade { get; set; } = null!;
    [Inject] private IUsersFacade UsersFacade { get; set; } = null!;
    [Inject] private ISettingClientFacade SettingClientFacade { get; set; } = null!;
    [Inject] private IGroupsFacade GroupsFacade { get; set; } = null!;
    [Inject] private IJSRuntime JavascriptRuntime { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private INotificationFactory NotificationFactory { get; set; } = null!;

    private ReportDefinitionModel? SelectedReport { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ReportsFacade.LoadReports();
        await UsersFacade.LoadAllUsers();
        if (!SettingClientFacade.SettingClients.Any())
            await SettingClientFacade.LoadAllClients(initializeScripts: false);

        _usernames = UsersFacade.UserCollection
            .Select(u => u.Username)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(u => u)
            .ToList();

        var groups = await GroupsFacade.GetAllGroups();
        _groupNames = groups
            .Select(g => g.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RefreshClientOptions();
        RefreshGroupOptions();

        _isLoading = false;
    }

    private async Task OnSelectedReportChanged(ReportDefinitionModel? report)
    {
        SelectedReport = report;
        _values.Clear();
        _settingNames.Clear();

        if (report is null)
            return;

        foreach (var parameter in report.Parameters)
            _values[parameter.Name] = parameter.Value;

        RefreshClientOptions();
        RefreshGroupOptions();
        await RefreshSettingNamesAsync();
    }

    private void RefreshClientOptions()
    {
        var options = SettingClientFacade.SettingClients
            .Select(c => new ClientOption(BuildClientKey(c), FormatClientDisplay(c)))
            .OrderBy(c => c.Display)
            .ToList();

        var clientParam = SelectedReport?.Parameters.FirstOrDefault(p =>
            p.LookupKind == ReportParameterLookupKind.Clients);

        if (clientParam is not null && !clientParam.Required)
            options.Insert(0, new ClientOption(string.Empty, "All clients"));

        _clientOptions = options;
    }

    private void RefreshGroupOptions()
    {
        var options = _groupNames
            .Select(n => new GroupOption(n, n))
            .ToList();

        var groupParam = SelectedReport?.Parameters.FirstOrDefault(p =>
            p.LookupKind == ReportParameterLookupKind.Groups);

        if (groupParam is not null && !groupParam.Required)
            options.Insert(0, new GroupOption(string.Empty, "All groups"));

        _groupOptions = options;
    }

    private async Task OnClientChanged(string parameterName, string? value)
    {
        SetString(parameterName, value);
        await RefreshSettingNamesAsync();
    }

    private async Task RefreshSettingNamesAsync()
    {
        _settingNames.Clear();
        if (SelectedReport is null)
            return;

        var clientParam = SelectedReport.Parameters.FirstOrDefault(p =>
            p.LookupKind == ReportParameterLookupKind.Clients);
        if (clientParam is null)
            return;

        var clientKey = GetString(clientParam.Name);
        if (string.IsNullOrWhiteSpace(clientKey))
            return;

        var parts = clientKey.Split('\u001f', 2);
        var name = parts[0];
        var instance = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : null;

        var client = SettingClientFacade.SettingClients.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Instance ?? string.Empty, instance ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (client is null)
            return;

        _settingNames = client.Settings
            .Select(s => s.Name)
            .OrderBy(s => s)
            .ToList();

        await InvokeAsync(StateHasChanged);
    }

    private bool ShouldShowParameter(ReportParameterModel parameter)
    {
        if (SelectedReport is null)
            return false;

        if (string.Equals(parameter.Name, "Instance", StringComparison.OrdinalIgnoreCase) &&
            SelectedReport.Parameters.Any(p => p.LookupKind == ReportParameterLookupKind.Clients))
        {
            return false;
        }

        return true;
    }

    private async Task GenerateAsync()
    {
        if (SelectedReport is null)
            return;

        if (!ValidateParameters(out var error))
        {
            NotificationService.Notify(NotificationFactory.Failure("Invalid Parameters", error));
            return;
        }

        _isGenerating = true;
        try
        {
            var parameters = BuildParameterDictionary();
            var html = await ReportsFacade.GenerateReport(SelectedReport.Id, parameters);
            if (string.IsNullOrWhiteSpace(html))
            {
                NotificationService.Notify(NotificationFactory.Failure("Report Failed", "No HTML was returned from the API."));
                return;
            }

            var opened = await JavascriptRuntime.InvokeAsync<bool>("openHtmlInNewTab", html);
            if (!opened)
            {
                NotificationService.Notify(NotificationFactory.Failure(
                    "Popup Blocked",
                    "Allow pop-ups for Fig to open the generated report."));
                return;
            }

            NotificationService.Notify(NotificationFactory.Success("Report Generated", "Opened in a new tab. Use Print to save as PDF."));
        }
        finally
        {
            _isGenerating = false;
        }
    }

    private bool ValidateParameters(out string error)
    {
        error = string.Empty;
        if (SelectedReport is null)
        {
            error = "No report selected.";
            return false;
        }

        foreach (var parameter in SelectedReport.Parameters.Where(p => p.Required))
        {
            if (!_values.TryGetValue(parameter.Name, out var value) || IsEmpty(value))
            {
                error = $"{parameter.DisplayName} is required.";
                return false;
            }
        }

        return true;
    }

    private Dictionary<string, object?> BuildParameterDictionary()
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (SelectedReport is null)
            return result;

        var clientResolvedFromLookup = false;
        foreach (var parameter in SelectedReport.Parameters)
        {
            _values.TryGetValue(parameter.Name, out var value);

            if (parameter.LookupKind == ReportParameterLookupKind.Clients && value is string clientKey)
            {
                if (string.IsNullOrWhiteSpace(clientKey))
                {
                    result["ClientName"] = null;
                    result["Instance"] = null;
                    clientResolvedFromLookup = true;
                    continue;
                }

                var parts = clientKey.Split('\u001f', 2);
                result["ClientName"] = parts[0];
                result["Instance"] = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : null;
                clientResolvedFromLookup = true;
                continue;
            }

            if (clientResolvedFromLookup &&
                string.Equals(parameter.Name, "Instance", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result[parameter.Name] = value;
        }

        return result;
    }

    private string? GetString(string name)
        => _values.TryGetValue(name, out var value) ? value?.ToString() : null;

    private void SetString(string name, string? value) => _values[name] = value;

    private DateTime? GetDate(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
            return null;
        if (value is DateTime dt)
            return dt;
        return DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private void SetDate(string name, DateTime? value) => _values[name] = value;

    private bool GetBool(string name)
        => _values.TryGetValue(name, out var value) && value is true;

    private void SetBool(string name, bool value) => _values[name] = value;

    private int? GetInt(string name)
    {
        if (!_values.TryGetValue(name, out var value) || value is null)
            return null;
        if (value is int i)
            return i;
        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private void SetInt(string name, int? value) => _values[name] = value;

    private static bool IsEmpty(object? value)
        => value is null || (value is string s && string.IsNullOrWhiteSpace(s));

    private static string BuildClientKey(SettingClientConfigurationModel client)
        => $"{client.Name}\u001f{client.Instance}";

    private static string FormatClientDisplay(SettingClientConfigurationModel client)
        => string.IsNullOrWhiteSpace(client.Instance) ? client.Name : $"{client.Name} [{client.Instance}]";

    private sealed record ClientOption(string Key, string Display);

    private sealed record GroupOption(string Key, string Display);
}
