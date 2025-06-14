using System.Data;
using System.Text;
using Fig.Common.ExtensionMethods;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.ImportExport;
using Fig.Web.Facades;
using Fig.Web.Factories;
using Fig.Web.MarkdownReport;
using Fig.Web.Models.ImportExport;
using Fig.Web.Pages.Setting;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class ImportExport
{
    private FigDataExportDataContract? _fullDataToImport;
    private FigValueOnlyDataExportDataContract? _valueOnlyDataToImport;
    private FigValueOnlyDataExportDataContract? _changeSetReferenceData;
    private bool _importInProgress;
    private bool _importIsInvalid;
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
            var data = await DataFacade.ExportSettings();

            if (data is not null)
            {
                data.Environment = Settings.Value.Environment;
                var text = JsonConvert.SerializeObject(data, JsonSettings.FigUserFacing);
                await DownloadExport(text, $"FigExport-{DateTime.Now:s}.json");
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

                data = await DataFacade.ExportValueOnlySettings(selectedClientIdentifiers, _excludeEnvironmentSpecific);
            }
            else
            {
                data = await DataFacade.ExportValueOnlySettings(_excludeEnvironmentSpecific);
            }

            if (data is not null)
            {
                data.Environment = Settings.Value.Environment;

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

    private void SettingsImportFileChanged(string? args)
    {
        _importIsInvalid = true;
        try
        {
            if (args == null)
                throw new Exception();

            var trimmed = args.Substring(args.IndexOf(',') + 1);
            var data = Convert.FromBase64String(trimmed);
            var decodedString = Encoding.UTF8.GetString(data);

            if (decodedString.TryParseJson(TypeNameHandling.Objects, out FigDataExportDataContract? fullImport) &&
                fullImport?.ImportType != ImportType.UpdateValues)
            {
                _fullDataToImport = fullImport ?? throw new DataException("Invalid input data");
                UpdateFullImportStatus();
            }
            else if (decodedString.TryParseJson(TypeNameHandling.None,
                         out FigValueOnlyDataExportDataContract? valueOnlyImport))
            {
                _valueOnlyDataToImport = valueOnlyImport ?? throw new DataException("Invalid input data");
                UpdateValueOnlyStatus();
            }

            UpdateStatus("Ready for import");
            _importIsInvalid = false;
        }
        catch (Exception e)
        {
            UpdateStatus($"Invalid Json Export file. {e}");
        }

        _importInProgress = true;
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
    }

    private async Task DownloadExport(string text, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await FileUtil.SaveAs(JavascriptRuntime, fileName, bytes);
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
}