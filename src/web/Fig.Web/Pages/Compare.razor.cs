using System.Text;
using Fig.Common.ExtensionMethods;
using Fig.Contracts.ImportExport;
using Fig.Web.Facades;
using Fig.Web.Models.Compare;
using Fig.Web.Services;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;

namespace Fig.Web.Pages;

public partial class Compare
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    private bool _isLoading;
    private string? _errorMessage;
    private IReadOnlyList<SettingCompareModel>? _cachedFilteredRows;
    private bool _filteredRowsDirty = true;

    [Inject] private ISettingCompareService CompareService { get; set; } = null!;

    [Inject] private ISettingClientFacade SettingClientFacade { get; set; } = null!;

    [Inject] private ICompareFacade CompareFacade { get; set; } = null!;

    [Inject] private TooltipService TooltipService { get; set; } = null!;

    [Inject] private NotificationService NotificationService { get; set; } = null!;

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [Inject] private IJSRuntime JavascriptRuntime { get; set; } = null!;

    private IReadOnlyList<SettingCompareModel>? CompareRows
    {
        get => CompareFacade.CompareRows;
        set
        {
            CompareFacade.CompareRows = value;
            InvalidateFilteredRows();
        }
    }

    private CompareStatisticsModel? Statistics
    {
        get => CompareFacade.Statistics;
        set => CompareFacade.Statistics = value;
    }

    private CompareFilterMode FilterMode
    {
        get => CompareFacade.FilterMode;
        set
        {
            if (CompareFacade.FilterMode == value)
                return;

            CompareFacade.FilterMode = value;
            InvalidateFilteredRows();
        }
    }

    private string ClientFilter
    {
        get => CompareFacade.ClientFilter;
        set
        {
            if (string.Equals(CompareFacade.ClientFilter, value, StringComparison.Ordinal))
                return;

            CompareFacade.ClientFilter = value;
            InvalidateFilteredRows();
        }
    }

    private IEnumerable<SettingCompareModel> FilteredRows
    {
        get
        {
            if (!_filteredRowsDirty && _cachedFilteredRows is not null)
                return _cachedFilteredRows;

            if (CompareRows == null)
            {
                _cachedFilteredRows = Array.Empty<SettingCompareModel>();
                _filteredRowsDirty = false;
                return _cachedFilteredRows;
            }

            IEnumerable<SettingCompareModel> rows = CompareRows;

            rows = FilterMode switch
            {
                CompareFilterMode.DifferencesOnly => rows.Where(r =>
                    r.Status is CompareStatus.Different or CompareStatus.OnlyInLive or CompareStatus.OnlyInExport),
                CompareFilterMode.MatchesOnly => rows.Where(r => r.Status == CompareStatus.Match),
                CompareFilterMode.NotCompared => rows.Where(r => r.Status == CompareStatus.NotCompared),
                _ => rows
            };

            if (!string.IsNullOrWhiteSpace(ClientFilter))
                rows = rows.Where(r => MatchesGlobalFilter(r, ClientFilter));

            _cachedFilteredRows = rows.ToList();
            _filteredRowsDirty = false;
            return _cachedFilteredRows;
        }
    }

    private void InvalidateFilteredRows()
    {
        _filteredRowsDirty = true;
    }

    private static bool MatchesGlobalFilter(SettingCompareModel row, string filter)
    {
        return row.ClientDisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || row.SettingName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || (row.LiveValue?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (row.ExportValue?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (row.LastChangedBy?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (row.LastChangeMessage?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (row.ExportChangedBy?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || (row.ExportChangeMessage?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private async Task OnFileSelected(string? base64Content)
    {
        if (base64Content is null)
        {
            CompareRows = null;
            Statistics = null;
            _errorMessage = null;
            return;
        }

        _isLoading = true;
        _errorMessage = null;
        CompareRows = null;
        Statistics = null;
        StateHasChanged();
        await Task.Delay(50); // allow spinner to render

        try
        {
            var commaIdx = base64Content.IndexOf(',');
            if (commaIdx < 0)
                throw new FormatException("Invalid data URL format.");

            var trimmed = base64Content.Substring(commaIdx + 1);
            var data = Convert.FromBase64String(trimmed);

            if (data.Length > MaxFileSizeBytes)
            {
                _errorMessage = "File too large (max 50 MB).";
                return;
            }

            string json;
            if (CompressionUtil.IsZipFile(data))
            {
                if (!CompressionUtil.TryDecompressFromZip(data, out var extracted) || extracted is null)
                {
                    _errorMessage = "Could not extract JSON from zip.";
                    return;
                }
                json = extracted;
            }
            else
            {
                json = Encoding.UTF8.GetString(data);
            }

            if (json.TryParseJson(TypeNameHandling.Objects, out FigDataExportDataContract? fullExportData) && fullExportData is not null)
            {
                var (rows, stats) = await CompareService.CompareAsync(fullExportData);
                CompareRows = rows;
                Statistics = stats;
            }
            else if (json.TryParseJson(TypeNameHandling.None, out FigValueOnlyDataExportDataContract? valueOnlyExportData) && valueOnlyExportData is not null)
            {
                var (rows, stats) = await CompareService.CompareAsync(valueOnlyExportData);
                CompareRows = rows;
                Statistics = stats;
            }
            else
            {
                _errorMessage = "File is not a valid Fig export (full or value-only).";
                return;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void OnFileError(UploadErrorEventArgs args)
    {
        _errorMessage = args.Message;
    }

    private void OnClientFilterInput(ChangeEventArgs e)
    {
        ClientFilter = e.Value?.ToString() ?? string.Empty;
        StateHasChanged();
    }

    private async Task NavigateToSetting(SettingCompareModel row)
    {
        // Find the matching client in the facade so Settings page selects it
        var client = SettingClientFacade.SettingClients
            .FirstOrDefault(c => c.Name == row.ClientName && c.Instance == row.Instance);

        if (client is null)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Client Not Found",
                Detail = $"Could not find client {row.ClientDisplayName} in live settings.",
                Duration = 3000
            });
            return;
        }

        if (client.Instance != null)
            SettingClientFacade.PendingExpandedClientName = client.Name;

        SettingClientFacade.SelectedSettingClient = client;

        // Find the setting so we can expand it and enable advanced visibility if needed
        var setting = client.Settings.FirstOrDefault(s => s.Name == row.SettingName);
        if (setting is not null)
        {
            if (setting.IsCompactView)
                setting.ToggleCompactView(false);

            if (setting.Advanced)
            {
                // Show advanced settings on every client so the setting becomes visible
                SettingClientFacade.SettingClients.ForEach(c => c.ShowAdvancedChanged(true));
            }
        }

        // Build the scroll id in the same format used by the settings page
        var scrollId = $"{row.ClientName}-{row.Instance}-{row.SettingName}";

        NavigationManager.NavigateTo("/");

        // Allow time for the settings page to render
        await Task.Delay(300);
        await JavascriptRuntime.InvokeVoidAsync("scrollIntoView", scrollId);
        await JavascriptRuntime.InvokeVoidAsync("highlightSetting", scrollId);
    }

    private void ApplyExportValue(SettingCompareModel row)
    {
        try
        {
            // For data grid settings, prefer the raw JSON so the facade can correctly
            // deserialize it into the typed data grid value.
            var valueToApply = row.RawExportJson ?? row.ExportValue;

            SettingClientFacade.ApplyPendingValueFromCompare(
                row.ClientName, row.Instance, row.SettingName, valueToApply);

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Pending Change Applied",
                Detail = $"{row.ClientName} / {row.SettingName}",
                Duration = 3000
            });
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Apply Failed",
                Detail = ex.Message,
                Duration = 5000
            });
        }
    }

    private void ShowTooltip(ElementReference element, string text)
    {
        TooltipService.Open(element, text, new TooltipOptions { Position = TooltipPosition.Top });
    }
}
