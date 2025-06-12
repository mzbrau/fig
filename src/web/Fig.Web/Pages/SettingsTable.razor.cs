using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fig.Web.Pages;

public partial class SettingsTable
{
    [Inject] 
    private ISettingClientFacade SettingClientFacade { get; set; } = null!;
    
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    private double _loadProgress;
    private string? _loadingMessage;
    private bool _isLoadingSettings;

    private List<ISetting> Settings => SettingClientFacade.SettingClients
        .SelectMany(a => a.Settings)
        .ToList();

    protected override async Task OnInitializedAsync()
    {
        SettingClientFacade.OnLoadProgressed += HandleLoadProgressed;
        
        if (SettingClientFacade.SettingClients.All(a => !a.IsDirty))
        {
            _isLoadingSettings = true;
            await SettingClientFacade.LoadAllClients();
            _isLoadingSettings = false;
        }

        await base.OnInitializedAsync();
    }

    private void HandleLoadProgressed(object? sender, (string clientName, double percent) progress)
    {
        _loadProgress = Math.Round(progress.percent);
        _loadingMessage = $"Loading {progress.clientName}...";
        StateHasChanged();
    }

    private async Task ExportCsv()
    {
        var csvContent = GenerateSettingsCsv();
        
        if (string.IsNullOrEmpty(csvContent))
            return;
        
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var filename = $"settings-export-{DateTime.Now:yyyy-MM-dd-HHmmss}.csv";
        var base64 = Convert.ToBase64String(csvBytes);
        
        await JsRuntime.InvokeVoidAsync("downloadCsvFile", base64, filename);
    }

    private string GenerateSettingsCsv()
    {
        var settings = Settings;
        if (!settings.Any())
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        
        // Headers - match the visible columns from the data grid
        var headers = new[] { "Client", "Instance", "Name", "Description", "Value", "Last Changed (Exact)", "Last Changed" };
        sb.AppendLine(string.Join(",", headers.Select(FormatCsvField)));

        // Data rows
        foreach (var setting in settings)
        {
            var rowValues = new[]
            {
                setting.ParentName,
                setting.ParentInstance,
                setting.DisplayName,
                setting.RawDescription,
                setting.StringValue,
                setting.LastChanged?.ToString("yyyy-MM-dd HH:mm:ss"),
                setting.LastChangedRelative
            };
            
            sb.AppendLine(string.Join(",", rowValues.Select(FormatCsvField)));
        }

        return sb.ToString();
    }

    private static string FormatCsvField(object? field)
    {
        if (field == null) return "\"\"";

        var valueString = field.ToString() ?? "";
        
        // Escape quotes by doubling them
        valueString = valueString.Replace("\"", "\"\"");
        
        // Always quote the field to handle commas, newlines, etc.
        return $"\"{valueString}\"";
    }
}