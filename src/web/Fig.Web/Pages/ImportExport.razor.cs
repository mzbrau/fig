using System.Data;
using System.IO;
using System.Text;
using Fig.Common.ExtensionMethods;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.ImportExport;
using Fig.Web.Facades;
using Fig.Web.Factories;
using Fig.Web.MarkdownReport;
using Fig.Web.Models.ImportExport;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class ImportExport : IDisposable
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private bool _disposed;
    private FigDataExportDataContract? _fullDataToImport;
    private FigValueOnlyDataExportDataContract? _valueOnlyDataToImport;
    private FigValueOnlyDataExportDataContract? _changeSetReferenceData;
    private bool _importInProgress;
    private bool _importIsInvalid;
    private bool _importFileProcessing = false;
    private bool _importOperationInProgress = false;
    private bool _changeSetFileIsInvalid = false;
    private string? _importStatus;
    private string? _changeSetStatus;
    private ImportType _importType;
    private bool _maskSecrets = true;
    private bool _includeSettingAnalysis = false;
    private bool _settingExportInProgress = false;
    private bool _valueOnlyExportInProgress = false;
    private bool _markdownExportInProgress = false;
    private bool _changeSetExportInProgress = false;
    private bool _changeSetFileSelected = false;

    private RadzenDataGrid<DeferredImportClientModel> _deferredClientGrid = null!;
    private bool _excludeEnvironmentSpecific;
    private bool _changeSetExcludeEnvironmentSpecific;
    private bool _includeLastChanged;
    private bool _includeLastChangedValueOnly;
    private bool _splitFiles;
    private bool _splitInitOnly;

    private List<DeferredImportClientModel> DeferredClients => DataFacade.DeferredClients;

    [Inject] public IDataFacade DataFacade { get; set; } = null!;

    [Inject] public IJSRuntime JavascriptRuntime { get; set; } = null!;

    [Inject] public ISettingClientFacade SettingClientFacade { get; set; } = null!;

    [Inject] public IMarkdownReportGenerator MarkdownReportGenerator { get; set; } = null!;

    [Inject] private IImportTypeFactory ImportTypeFactory { get; set; } = null!;

    [Inject] private IOptions<WebSettings> Settings { get; set; } = null!;

    private List<ImportTypeEnumerable> ImportTypes { get; } = new();

    private ValueOnlyExportMode _valueOnlyExportMode = ValueOnlyExportMode.AllClients;
    private List<ClientSelectionModel> _availableClients = new();
    private List<ClientSelectionModel> _filteredClients = new();
    private string _clientFilter = string.Empty;
    private bool _clientSelectionVisible = false;
    private bool _loadingClients = false;

    private bool SplitFiles
    {
        get => _splitFiles;
        set
        {
            _splitFiles = value;
            if (!value)
            {
                _splitInitOnly = false;
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        foreach (var item in ImportTypeFactory.GetImportTypes())
        {
            ImportTypes.Add(item);
        }

        await DataFacade.RefreshDeferredClients();
        await base.OnInitializedAsync();
    }

    private async Task PerformSettingsImport()
    {
        if (_fullDataToImport is null && _valueOnlyDataToImport is null)
            return;

        _importOperationInProgress = true;
        try
        {
            ImportResultDataContract? result;
            if (_fullDataToImport is not null)
            {
                _fullDataToImport.ImportType = _importType;
                UpdateStatus("Starting full import...");
                result = await DataFacade.ImportSettings(_fullDataToImport);
            }
            else
            {
                UpdateStatus("Starting value only import...");
                result = await DataFacade.ImportValueOnlySettings(_valueOnlyDataToImport!);
            }

            if (result is not null && result.ErrorMessage is null)
            {
                UpdateStatus("Import Completed Successfully");
                UpdateStatus($"Import Type: {result.ImportType}");
                UpdateStatus($"{result.DeletedClients.Count} clients removed.");
                PrintClients(result.DeletedClients);

                UpdateStatus($"{result.ImportedClients.Count} clients added / updated.");
                PrintClients(result.ImportedClients);

                UpdateStatus($"{result.DeferredImportClients.Count} deferred client imports.");
                PrintClients(result.DeferredImportClients);

                UpdateStatus(
                    $"Added the following:{Environment.NewLine}{string.Join(Environment.NewLine, result.ImportedClients)}");

                if (result.DeletedClients.Count > 0 || result.ImportedClients.Count > 0)
                    await SettingClientFacade.LoadAllClients();
            }
            else if (result is not null)
            {
                UpdateStatus("Import Failed");
                UpdateStatus(result.ErrorMessage!);
            }
            else
            {
                UpdateStatus("Import Failed");
            }

            await DataFacade.RefreshDeferredClients();

            _fullDataToImport = null;
            _valueOnlyDataToImport = null;
        }
        finally
        {
            _importOperationInProgress = false;
        }
    }

    private void PrintClients(List<string> names)
    {
        if (!names.Any())
            return;

        foreach (var name in names)
        {
            UpdateStatus(name);
        }

        UpdateStatus(string.Empty);
    }

    private async Task PerformSettingsExport()
    {
        _settingExportInProgress = true;
        try
        {
            var data = await DataFacade.ExportSettings(_includeLastChanged);

            if (data is not null)
            {
                data.Environment = Settings.Value.Environment;
                var text = JsonConvert.SerializeObject(data, JsonSettings.FigUserFacing);
                
                // Compress full exports to reduce file size
                var zipBytes = CompressionUtil.CompressToZip(text, $"FigExport-{DateTime.Now:s}.json");
                await FileUtil.SaveAs(JavascriptRuntime, $"FigExport-{DateTime.Now:s}.zip", zipBytes);
            }
        }
        finally
        {
            _settingExportInProgress = false;
        }
    }

    private async Task PerformValueOnlySettingsExport()
    {
        _valueOnlyExportInProgress = true;
        try
        {
            FigValueOnlyDataExportDataContract? data;

            if (_valueOnlyExportMode == ValueOnlyExportMode.SelectClients)
            {
                // Get selected client identifiers
                var selectedClientIdentifiers = _availableClients
                    .Where(c => c.IsSelected)
                    .Select(c => c.Identifier)
                    .ToList();

                if (!selectedClientIdentifiers.Any())
                {
                    // If no clients selected, don't export anything
                    return;
                }

                data = await DataFacade.ExportValueOnlySettings(selectedClientIdentifiers, _excludeEnvironmentSpecific, _includeLastChangedValueOnly);
            }
            else
            {
                data = await DataFacade.ExportValueOnlySettings(_excludeEnvironmentSpecific, _includeLastChangedValueOnly);
            }

            if (data is not null)
            {
                data.Environment = Settings.Value.Environment;

                if (_splitFiles)
                {
                    var splitExports = BuildSplitValueOnlyExports(data);
                    if (splitExports.Any())
                    {
                        var zipEntries = splitExports.ToDictionary(x => x.FileName, x => x.Json);

                        var zipBytes = CompressionUtil.CompressToZip(zipEntries);
                        await FileUtil.SaveAs(JavascriptRuntime, $"FigValueOnlyExport-{DateTime.Now:s}.zip", zipBytes);
                        return;
                    }
                }

                var text = JsonConvert.SerializeObject(data, JsonSettings.FigMinimalUserFacing);
                await DownloadExport(text, $"FigValueOnlyExport-{DateTime.Now:s}.json");
            }
        }
        finally
        {
            _valueOnlyExportInProgress = false;
        }
    }

    private async Task PerformSettingsReport()
    {
        _markdownExportInProgress = true;
        try
        {
            var data = await DataFacade.ExportSettings();
            if (data != null)
            {
                var text = MarkdownReportGenerator.GenerateReport(data, _maskSecrets, _includeSettingAnalysis);
                await DownloadExport(text, $"FigReport-{DateTime.Now:s}.md");
            }
        }
        finally
        {
            _markdownExportInProgress = false;
        }
    }

    private async Task PerformChangeSetExport()
    {
        if (_changeSetReferenceData == null)
            return;

        _changeSetExportInProgress = true;
        try
        {
            var data = await DataFacade.ExportChangeSetSettings(_changeSetReferenceData,
                _changeSetExcludeEnvironmentSpecific);
            if (data is not null)
            {
                data.Environment = Settings.Value.Environment;

                var text = JsonConvert.SerializeObject(data, JsonSettings.FigMinimalUserFacing);
                await DownloadExport(text, $"FigChangeSetExport-{DateTime.Now:s}.json");
            }
        }
        finally
        {
            _changeSetExportInProgress = false;
        }
    }

    private async Task SettingsImportFileChanged(string? args)
    {
        if (args is null)
        {
            _importInProgress = false;
            _importFileProcessing = false;
            return;
        }

        // Set processing state and update UI
        _importFileProcessing = true;
        _importIsInvalid = true;
        _importStatus = string.Empty;
        _importInProgress = false;
        _fullDataToImport = null;
        _valueOnlyDataToImport = null;
        await InvokeAsync(StateHasChanged);
        
        // Small delay to allow spinner to render
        await Task.Delay(50);
        
        try
        {
            var commaIdx = args.IndexOf(',');
            if (commaIdx < 0)
                throw new FormatException("Invalid data URL format.");
            var trimmed = args.Substring(commaIdx + 1);
            var data = Convert.FromBase64String(trimmed);
            
            // Check file size
            if (data.Length > MaxFileSizeBytes)
            {
                var fileSizeMb = Math.Round(data.Length / (1024.0 * 1024.0), 2);
                var maxFileSizeMb = Math.Round(MaxFileSizeBytes / (1024.0 * 1024.0), 2);
                if (CompressionUtil.IsZipFile(data))
                {
                    UpdateStatus($"File is too large ({fileSizeMb} MB). Maximum allowed size is {maxFileSizeMb} MB.");
                    UpdateStatus("This file is already compressed. Please split the import into smaller parts.");
                }
                else
                {
                    UpdateStatus($"File is too large ({fileSizeMb} MB). Maximum allowed size is {maxFileSizeMb} MB.");
                    UpdateStatus("Please compress the file as a .zip before importing.");
                }
                _importFileProcessing = false;
                _importInProgress = true;
                await InvokeAsync(StateHasChanged);
                return;
            }
            
            string decodedString;
            
            // Check if the file is a zip and decompress if needed
            if (CompressionUtil.IsZipFile(data))
            {
                if (CompressionUtil.TryDecompressFromZip(data, out var extractedContent))
                {
                    decodedString = extractedContent!;
                    UpdateStatus("Successfully extracted JSON from zip file.");
                }
                else
                {
                    var jsonEntryCount = CompressionUtil.CountJsonEntriesInZip(data);
                    if (jsonEntryCount > 1)
                    {
                        UpdateStatus("This zip contains multiple export JSON files.");
                        UpdateStatus("Split-file exports are not imported as a single zip. Extract the files and import them one at a time.");
                    }
                    else
                    {
                        UpdateStatus("Invalid or corrupted zip file. Could not extract JSON content.");
                        UpdateStatus("Please ensure the zip file contains exactly one valid Fig export JSON file.");
                    }
                    _importFileProcessing = false;
                    _importInProgress = true;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
            }
            else
            {
                decodedString = Encoding.UTF8.GetString(data);
            }

            // Try to parse as full import
            if (decodedString.TryParseJson(TypeNameHandling.Objects, out FigDataExportDataContract? fullImport) &&
                fullImport?.ImportType != ImportType.UpdateValues)
            {
                _fullDataToImport = fullImport ?? throw new DataException("Invalid input data");
                UpdateFullImportStatus();
            }
            // Try to parse as value-only import
            else if (decodedString.TryParseJson(TypeNameHandling.None,
                         out FigValueOnlyDataExportDataContract? valueOnlyImport))
            {
                _valueOnlyDataToImport = valueOnlyImport ?? throw new DataException("Invalid input data");
                UpdateValueOnlyStatus();
            }
            else
            {
                UpdateStatus("Invalid import file format.");
                UpdateStatus("The file does not contain a valid Fig export (full or value-only).");
                UpdateStatus("Please ensure you are uploading a file exported from Fig.");
                _importFileProcessing = false;
                _importInProgress = true;
                await InvokeAsync(StateHasChanged);
                return;
            }

            if (_fullDataToImport?.Clients.Count > 0 || _valueOnlyDataToImport?.Clients.Count > 0)
            {
                UpdateStatus("Ready for import");
                _importIsInvalid = false;
            }
            else
            {
                UpdateStatus("No clients to import");
            }
        }
        catch (FormatException)
        {
            UpdateStatus("Invalid file encoding. The file must be a valid base64-encoded JSON or zip file.");
        }
        catch (JsonException ex)
        {
            UpdateStatus("Invalid JSON format in import file.");
            UpdateStatus($"JSON parsing error: {ex.Message}");
            UpdateStatus("Please ensure the file is a valid Fig export.");
        }
        catch (Exception e)
        {
            UpdateStatus($"Error processing import file: {e.Message}");
            UpdateStatus("Please verify the file is a valid Fig export (JSON or zip format).");
        }
        finally
        {
            _importFileProcessing = false;
            _importInProgress = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void UpdateFullImportStatus()
    {
        _importType = _fullDataToImport!.ImportType;
        UpdateStatus(
            $"File was export at {_fullDataToImport.ExportedAt.ToLocalTime()} with version {_fullDataToImport.Version}");
        UpdateStatus($"Import contains {_fullDataToImport.Clients.Count} client(s).");
        foreach (var client in _fullDataToImport.Clients)
            UpdateStatus(
                $"{client.Name} -> {client.Settings.Count} settings");
    }

    private void UpdateValueOnlyStatus()
    {
        _importType = _valueOnlyDataToImport!.ImportType;
        UpdateStatus(
            $"File was export at {_valueOnlyDataToImport.ExportedAt.ToLocalTime()} with version {_valueOnlyDataToImport.Version}");
        UpdateStatus($"Import contains {_valueOnlyDataToImport.Clients.Count} client(s).");
        foreach (var client in _valueOnlyDataToImport.Clients)
            UpdateStatus(
                $"{client.Name} -> {client.Settings.Count} settings");
    }

    private void OnImportFileError(UploadErrorEventArgs args)
    {
        UpdateStatus(args.Message);
    }

    private void UpdateStatus(string text)
    {
        _importStatus += text + Environment.NewLine;
        _ = InvokeAsyncSafely(async () =>
        {
            StateHasChanged();
            await Task.Delay(10);
            await JavascriptRuntime.InvokeVoidAsync("scrollTextAreaToBottom", "importStatusTextArea");
        });
    }

    private async Task InvokeAsyncSafely(Func<Task> action)
    {
        if (_disposed)
            return;

        try
        {
            await InvokeAsync(action);
        }
        catch (ObjectDisposedException)
        {
            // Component was disposed during async operation - this is expected
        }
        catch (JSDisconnectedException)
        {
            // JS runtime no longer available (navigation/disconnect) – safe to ignore
        }
    }

    private async Task DownloadExport(string text, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await FileUtil.SaveAs(JavascriptRuntime, fileName, bytes);
    }

    private List<(string FileName, string Json)> BuildSplitValueOnlyExports(FigValueOnlyDataExportDataContract source)
    {
        var exports = new List<(string FileName, string Json)>();
        var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var client in source.Clients)
        {
            var clientFilePrefix = GetClientExportFilePrefix(client.Name, client.Instance);

            if (!_splitInitOnly)
            {
                var export = CreateSingleClientValueOnlyExport(source, client, client.Settings, source.ImportType);
                var json = JsonConvert.SerializeObject(export, JsonSettings.FigMinimalUserFacing);
                AddSplitExport(exports, fileNames, $"{clientFilePrefix}.json", json);
                continue;
            }

            var initOnlySettings = client.Settings.Where(s => s.InitOnlyExport == true).ToList();
            if (initOnlySettings.Any())
            {
                var initOnlyClient = new SettingClientValueExportDataContract(client.Name, client.Instance, initOnlySettings);
                var initOnlyExport = CreateSingleClientValueOnlyExport(source, initOnlyClient, initOnlySettings, ImportType.UpdateValuesInitOnly);
                var initOnlyJson = JsonConvert.SerializeObject(initOnlyExport, JsonSettings.FigMinimalUserFacing);
                AddSplitExport(exports, fileNames, $"{clientFilePrefix}-init-only.json", initOnlyJson);
            }

            var updateSettings = client.Settings.Where(s => s.InitOnlyExport != true).ToList();
            if (updateSettings.Any())
            {
                var updateClient = new SettingClientValueExportDataContract(client.Name, client.Instance, updateSettings);
                var updateExport = CreateSingleClientValueOnlyExport(source, updateClient, updateSettings, ImportType.UpdateValues);
                var updateJson = JsonConvert.SerializeObject(updateExport, JsonSettings.FigMinimalUserFacing);
                AddSplitExport(exports, fileNames, $"{clientFilePrefix}-update-values.json", updateJson);
            }
        }

        return exports;
    }

    private static void AddSplitExport(
        List<(string FileName, string Json)> exports,
        HashSet<string> fileNames,
        string fileName,
        string json)
    {
        var uniqueFileName = fileName;
        var sequence = 1;
        while (!fileNames.Add(uniqueFileName))
        {
            sequence++;
            var extension = Path.GetExtension(fileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            uniqueFileName = $"{fileNameWithoutExtension}-{sequence}{extension}";
        }

        exports.Add((uniqueFileName, json));
    }

    private static FigValueOnlyDataExportDataContract CreateSingleClientValueOnlyExport(
        FigValueOnlyDataExportDataContract source,
        SettingClientValueExportDataContract client,
        List<SettingValueExportDataContract> settings,
        ImportType importType)
    {
        var clientExport = new SettingClientValueExportDataContract(client.Name, client.Instance, settings);
        return new FigValueOnlyDataExportDataContract(
            source.ExportedAt,
            importType,
            source.Version,
            source.IsExternallyManaged,
            [clientExport])
        {
            ExportingServer = source.ExportingServer,
            Environment = source.Environment
        };
    }

    private static string GetClientExportFilePrefix(string clientName, string? instance)
    {
        var sanitizedClient = SanitizeFileNamePart(clientName);
        if (string.IsNullOrWhiteSpace(instance))
        {
            return sanitizedClient;
        }

        return $"{sanitizedClient}-{SanitizeFileNamePart(instance)}";
    }

    private static string SanitizeFileNamePart(string value)
    {
        var sanitized = value;
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidChar, '-');
        }

        sanitized = sanitized.Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "unnamed";
        }

        return sanitized;
    }

    private void ChangeSetReferenceFileChanged(string? args)
    {
        _changeSetFileIsInvalid = true;
        _changeSetStatus = string.Empty;
        try
        {
            if (args == null)
            {
                _changeSetFileIsInvalid = false;
                _changeSetFileSelected = false;
                UpdateChangeSetStatus();
                return;
            }


            var trimmed = args.Substring(args.IndexOf(',') + 1);
            var data = Convert.FromBase64String(trimmed);
            var decodedString = Encoding.UTF8.GetString(data);

            if (decodedString.TryParseJson(TypeNameHandling.None,
                    out FigValueOnlyDataExportDataContract? valueOnlyData))
            {
                _changeSetReferenceData = valueOnlyData ?? throw new DataException("Invalid input data");
                UpdateChangeSetStatus();
                _changeSetFileIsInvalid = false;
                _changeSetFileSelected = true;
            }
            else
            {
                throw new Exception("Invalid JSON format for value only export");
            }
        }
        catch (Exception e)
        {
            UpdateChangeSetStatus($"Invalid value only export file. {e.Message}");
            _changeSetFileSelected = true; // Keep selected state to show error
        }
    }

    private void UpdateChangeSetStatus(string? errorMessage = null)
    {
        if (errorMessage != null)
        {
            _changeSetStatus = errorMessage;
            return;
        }

        if (_changeSetReferenceData != null)
        {
            _changeSetStatus = $"Reference file loaded successfully.\n";
            _changeSetStatus += $"File was exported at {_changeSetReferenceData.ExportedAt.ToLocalTime()}\n";
            _changeSetStatus += $"Import contains {_changeSetReferenceData.Clients.Count} client(s).\n";
            foreach (var client in _changeSetReferenceData.Clients)
                _changeSetStatus += $"{client.Name} -> {client.Settings.Count} settings\n";
            _changeSetStatus += "Ready to generate change set export.";
        }
    }

    private void OnChangeSetFileError(UploadErrorEventArgs args)
    {
        UpdateChangeSetStatus(args.Message);
    }

    private enum ValueOnlyExportMode
    {
        AllClients,
        SelectClients
    }

    private async Task OnValueOnlyExportModeChanged(ValueOnlyExportMode mode)
    {
        _valueOnlyExportMode = mode;
        _clientSelectionVisible = mode == ValueOnlyExportMode.SelectClients;

        if (_clientSelectionVisible && !_availableClients.Any())
        {
            await LoadAvailableClients();
        }
    }

    private async Task LoadAvailableClients()
    {
        _loadingClients = true;
        try
        {
            // get the clients from the existing export
            var data = await DataFacade.ExportValueOnlySettings(false);
            if (data != null)
            {
                _availableClients = data.Clients.Select(client =>
                        new ClientSelectionModel(
                            client.Name,
                            client.Instance,
                            client.Settings.Count,
                            false))
                    .OrderBy(c => c.Name)
                    .ThenBy(c => c.Instance)
                    .ToList();

                UpdateFilteredClients();
            }
        }
        finally
        {
            _loadingClients = false;
        }
    }

    private void UpdateFilteredClients()
    {
        if (string.IsNullOrWhiteSpace(_clientFilter))
        {
            _filteredClients = _availableClients.ToList();
        }
        else
        {
            _filteredClients = _availableClients
                .Where(c => c.DisplayName.Contains(_clientFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private void OnClientFilterInput(ChangeEventArgs args)
    {
        _clientFilter = args.Value?.ToString() ?? string.Empty;
        UpdateFilteredClients();
    }

    private void SelectAllClients()
    {
        foreach (var client in _filteredClients)
        {
            client.IsSelected = true;
        }
    }

    private void SelectNoClients()
    {
        foreach (var client in _filteredClients)
        {
            client.IsSelected = false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
