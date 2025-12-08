using System.Text;
using System.Text.RegularExpressions;
using Fig.Common.NetStandard.Scripting;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Constants;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages.Setting.SettingEditors.DataGrid;

public partial class DataGridSetting
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private RadzenDataGrid<Dictionary<string, IDataGridValueModel>> _settingGrid = null!;
    private InputFile? _inputFile;
    private IBrowserFile? _selectedFile;
    private string _searchText = string.Empty;
    private int? _cachedFilteredCount;
    private List<List<string>>? _cachedLowercaseRowValues;

    [Parameter]
    public DataGridSettingConfigurationModel Setting { get; set; } = null!;

    [Inject] 
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject] 
    private NotificationService NotificationService { get; set; } = null!;

    private string SanitizeIdString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "dataGrid";
        
        // Replace any character that's not A-Z, a-z, 0-9, hyphen, underscore, or period with underscore
        return Regex.Replace(input, @"[^A-Za-z0-9\-_\.]", "_", RegexOptions.None, RegexConstants.DefaultTimeout);
    }

    private void SetValue(Dictionary<string, object> context, string key, object value)
    {
        context[key] = value;
    }

    private async Task EditRow(Dictionary<string, IDataGridValueModel> row)
    {
        await _settingGrid.EditRow(row);
        Setting.RunDisplayScript();
    }

    private async Task SaveRow(Dictionary<string, IDataGridValueModel> row)
    {
        foreach (var item in row.Values)
            item.RowSaved();

        await _settingGrid.UpdateRow(row);
        Setting.EvaluateDirty();
        await Task.Run(() => Setting.ValidateDataGrid());
        Setting.RunDisplayScript();
        InvalidateCaches();
    }

    private void CancelEdit(Dictionary<string, IDataGridValueModel> row)
    {
        foreach (var item in row.Values)
            item.RevertRowChanged();

        _settingGrid.CancelEditRow(row);
    }

    private async Task DeleteRow(Dictionary<string, IDataGridValueModel> row)
    {
        Setting.Value?.Remove(row);
        InvalidateCaches();
        await _settingGrid.Reload();
        Setting.EvaluateDirty();
        Setting.RunDisplayScript();
    }

    private async Task InsertRow()
    {
        var rowToInsert = Setting.CreateRow(Setting);
        if (rowToInsert is not null)
        {
            Setting.Value?.Add(rowToInsert);
            InvalidateCaches();
            await _settingGrid.Reload();
            Setting.EvaluateDirty();
            await EditRow(rowToInsert);
            Setting.RunDisplayScript();
        }
    }

    private void InvalidateCaches()
    {
        _cachedLowercaseRowValues = null;
        _cachedFilteredCount = null;
    }
    
    private string FormatColumnName(string columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return columnName;
        return Regex.Replace(columnName, "([A-Z])", " $1", RegexOptions.None, RegexConstants.DefaultTimeout).Trim();
    }

    private List<Dictionary<string, IDataGridValueModel>>? GetFilteredData()
    {
        if (Setting.Value == null)
            return null;

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            _cachedFilteredCount = null;
            return Setting.Value;
        }

        var searchTerms = _searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        if (!searchTerms.Any())
        {
            _cachedFilteredCount = null;
            return Setting.Value;
        }

        // Cache lowercase row values on first search
        if (_cachedLowercaseRowValues == null)
        {
            _cachedLowercaseRowValues = Setting.Value.Select(row =>
                row.Values
                    .Where(a => !a.IsSecret)
                    .Select(v => GetSearchableValue(v.ReadOnlyValue))
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Select(v => v!.ToLowerInvariant())
                    .ToList()
            ).ToList();
        }

        var filteredRows = new List<Dictionary<string, IDataGridValueModel>>();
        for (int i = 0; i < Setting.Value.Count; i++)
        {
            var rowValues = _cachedLowercaseRowValues[i];
            if (searchTerms.All(term => rowValues.Any(value => value.Contains(term))))
            {
                filteredRows.Add(Setting.Value[i]);
            }
        }

        _cachedFilteredCount = filteredRows.Count;
        return filteredRows;
    }

    private int GetFilteredCount()
    {
        // Return cached count if available
        if (_cachedFilteredCount.HasValue)
            return _cachedFilteredCount.Value;

        // If no search text, return total count
        if (string.IsNullOrWhiteSpace(_searchText))
            return Setting.Value?.Count ?? 0;

        // Trigger filtering which will cache the count
        GetFilteredData();
        return _cachedFilteredCount ?? 0;
    }

    private string? GetSearchableValue(object? value)
    {
        if (value == null)
            return null;

        if (value is IEnumerable<string> stringList)
            return string.Join(", ", stringList);

        return value.ToString();
    }

    private async Task OnSearchTextChanged(ChangeEventArgs e)
    {
        _searchText = e.Value?.ToString() ?? string.Empty;
        _cachedFilteredCount = null; // Invalidate count cache
        await OnSearchChanged();
    }

    private async Task OnSearchChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearSearch()
    {
        _searchText = string.Empty;
        _cachedFilteredCount = null; // Invalidate count cache
        await OnSearchChanged();
    }

    protected override void OnInitialized()
    {
        Setting.SubscribeToValueChanges(HandleValueChange);
        base.OnInitialized();
    }

    private async void HandleValueChange(ActionType actionType)
    {
        InvalidateCaches();
        await _settingGrid.Reload();
        StateHasChanged();
    }

    private async Task ExportCsv()
    {
        var csvContent = DataGridCsvHandler.ConvertToCsv(Setting);

        if (csvContent is null)
            return;
        
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        var filename = $"{Setting.Parent.Name}-{Setting.Name}-export.csv";
        var base64 = Convert.ToBase64String(csvBytes);
        
        await JsRuntime.InvokeVoidAsync("downloadCsvFile", base64, filename);
        
        Console.WriteLine("Export CSV completed");
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        _selectedFile = e.File;
        var configuredColumns = Setting.DataGridConfiguration?.Columns;

        if (_selectedFile == null || !IsFileSizeValid(_selectedFile) || !DoColumnsExist())
        {
            return;
        }
        
        string csvContent;
        using (var memoryStream = new MemoryStream())
        {
            await _selectedFile.OpenReadStream(MaxFileSize).CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            using (var reader = new StreamReader(memoryStream))
            {
                csvContent = await reader.ReadToEndAsync();
            }
        }

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error, 
                Summary = "Import Failed",
                Detail = "CSV file is empty or header row is missing.", 
                Duration = 5000
            });
            return;
        }

        var result = DataGridCsvHandler.ParseCsvToRows(
            csvContent,
            configuredColumns!,
            CreateValueModel,
            Setting);
        
        if (result.Errors.Any())
        {
            var summary = $"Import failed with {result.Errors.Count} error(s).";
            var detail = string.Join("\n", result.Errors.Take(10));
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error, 
                Summary = summary, 
                Detail = detail, 
                Duration = 15000
            });
            return;
        }
        
        if (!result.Rows.Any())
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning, 
                Summary = "Import Warning",
                Detail = "No data rows were successfully imported. Please check file content and configuration.",
                Duration = 7000
            });
            return;
        }
        
        if (Setting.Value != null)
        {
            Setting.Value.Clear();
            Setting.Value.AddRange(result.Rows);
        }
        else
        {
            Setting.Value = new List<Dictionary<string, IDataGridValueModel>>(result.Rows);
        }
        
        InvalidateCaches();
        Setting.EvaluateDirty();
        NotificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success, 
            Summary = "Import Successful",
            Detail = $"{result.Rows.Count} rows imported.", 
            Duration = 5000
        });
        
        await InvokeAsync(StateHasChanged);
        if (_inputFile != null)
        {
            await JsRuntime.InvokeVoidAsync("resetFileInputValueById", $"dataGridCsvInputFile_{SanitizeIdString(Setting.Name)}");
        }
        
        bool IsFileSizeValid(IBrowserFile file)
        {
            var isValid = file.Size <= MaxFileSize;
            if (!isValid)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Import Failed",
                    Detail = $"File size exceeds the maximum limit of 10MB.",
                    Duration = 5000
                });
            }
            
            return isValid;
        }
        
        bool DoColumnsExist()
        {
            if (configuredColumns == null || !configuredColumns.Any())
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Import Failed",
                    Detail = "Data grid is not configured for import.",
                    Duration = 5000
                });
                return false;
            }
            return true;
        }
    }

    private IDataGridValueModel CreateValueModel(Type systemType, object? value, IDataGridColumn columnConfig, ISetting parentSetting)
    {
        var genericModelBaseType = typeof(DataGridValueModel<>);
        var specificModelType = genericModelBaseType.MakeGenericType(systemType);

        return (IDataGridValueModel)Activator.CreateInstance(specificModelType,
            value,
            columnConfig.IsReadOnly,
            parentSetting,
            columnConfig.ValidValues?.ToList(), 
            columnConfig.EditorLineCount,
            columnConfig.ValidationRegex,
            columnConfig.ValidationExplanation,
            columnConfig.IsSecret
        )!;
    }
}