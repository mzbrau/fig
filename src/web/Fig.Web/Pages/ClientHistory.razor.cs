using Fig.Common.NetStandard.Json;
using Fig.Web.Facades;
using Fig.Web.Models.ClientHistory;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class ClientHistory
{
    private bool _isLoading;
    private string _clientFilter = string.Empty;
    private string _settingFilter = string.Empty;
    private string? _selectedClient;
    private RadzenUpload _fileUpload = null!;
    private List<string> _allSettingNamesCache = new();
    private bool _allSettingNamesDirty = true;

    [Inject]
    private IClientRegistrationHistoryFacade Facade { get; set; } = null!;

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;

    private List<string> FilteredClientNames => string.IsNullOrWhiteSpace(_clientFilter)
        ? Facade.UniqueClientNames
        : Facade.UniqueClientNames
            .Where(n => n.Contains(_clientFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private List<ClientRegistrationHistoryModel> SelectedClientRegistrations =>
        string.IsNullOrEmpty(_selectedClient)
            ? new List<ClientRegistrationHistoryModel>()
            : Facade.GetRegistrationsForClient(_selectedClient);

    private List<ImportedClientDefinition> SelectedClientImports =>
        string.IsNullOrEmpty(_selectedClient)
            ? new List<ImportedClientDefinition>()
            : Facade.GetImportedDefinitionsForClient(_selectedClient);

    private List<ImportedClientDefinition> ImportedDefinitions => Facade.ImportedDefinitions;

    private List<string> AllSettingNames
    {
        get
        {
            if (_allSettingNamesDirty)
            {
                _allSettingNamesCache = BuildAllSettingNames();
                _allSettingNamesDirty = false;
            }

            return _allSettingNamesCache;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        await base.OnInitializedAsync();
    }

    private async Task LoadData()
    {
        _isLoading = true;
        try
        {
            await Facade.LoadHistory();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationFactory.Failure("Load Failed", ex.Message));
        }
        finally
        {
            _isLoading = false;
            MarkAllSettingNamesDirty();
        }
    }

    private async Task OnRefresh()
    {
        await LoadData();
    }

    private void OnClientFilterInput(ChangeEventArgs args)
    {
        _clientFilter = args.Value?.ToString() ?? string.Empty;
    }

    private void OnSettingFilterInput(ChangeEventArgs args)
    {
        _settingFilter = args.Value?.ToString() ?? string.Empty;
        MarkAllSettingNamesDirty();
    }

    private void SelectClient(string clientName)
    {
        _selectedClient = clientName;
        MarkAllSettingNamesDirty();
    }

    private bool HasImportedDefinitions(string clientName)
    {
        return Facade.GetImportedDefinitionsForClient(clientName).Any();
    }

    private async Task OnFilesSelected(UploadChangeEventArgs args)
    {
        foreach (var file in args.Files)
        {
            try
            {
                await ProcessImportedFile(file);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationFactory.Failure($"Import Failed: {file.Name}", ex.Message));
            }
        }
        
        StateHasChanged();
    }

    private async Task ProcessImportedFile(Radzen.FileInfo file)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var definition = JsonConvert.DeserializeObject<ImportedClientDefinition>(json, JsonSettings.FigDefault);

        if (definition == null)
        {
            throw new InvalidOperationException("Failed to parse JSON file. The file format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(definition.ClientName))
        {
            throw new InvalidOperationException("The JSON file does not contain a valid ClientName.");
        }

        if (definition.Settings == null || !definition.Settings.Any())
        {
            throw new InvalidOperationException("The JSON file does not contain any settings.");
        }

        Facade.AddImportedDefinition(definition);
        MarkAllSettingNamesDirty();
        NotificationService.Notify(NotificationFactory.Success("Import Successful", $"Imported {definition.ClientName} v{definition.ClientVersion}"));
    }

    private void ClearImportedDefinitions()
    {
        Facade.ClearImportedDefinitions();
        MarkAllSettingNamesDirty();
        NotificationService.Notify(NotificationFactory.Success("Cleared", "All imported definitions have been cleared."));
    }

    private List<string> BuildAllSettingNames()
    {
        var settingNames = new HashSet<string>();

        foreach (var reg in SelectedClientRegistrations)
        {
            foreach (var setting in reg.Settings)
            {
                settingNames.Add(setting.Name);
            }
        }

        foreach (var import in SelectedClientImports)
        {
            foreach (var setting in import.Settings)
            {
                settingNames.Add(setting.Name);
            }
        }

        var orderedNames = settingNames.OrderBy(n => n).ToList();

        if (string.IsNullOrWhiteSpace(_settingFilter))
            return orderedNames;

        return orderedNames
            .Where(n => n.Contains(_settingFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void MarkAllSettingNamesDirty()
    {
        _allSettingNamesDirty = true;
    }

    private bool GetAdvancedSetting(string settingName)
    {
        var latestDate = DateTime.MinValue;
        var advanced = false;

        foreach (var reg in SelectedClientRegistrations)
        {
            var setting = reg.Settings.FirstOrDefault(s => s.Name == settingName);
            if (setting != null && reg.RegistrationDateUtc > latestDate)
            {
                latestDate = reg.RegistrationDateUtc;
                advanced = setting.Advanced;
            }
        }

        foreach (var import in SelectedClientImports)
        {
            var setting = import.Settings.FirstOrDefault(s => s.Name == settingName);
            if (setting != null && import.GeneratedDateUtc > latestDate)
            {
                latestDate = import.GeneratedDateUtc;
                advanced = setting.Advanced;
            }
        }

        return advanced;
    }

    private static string FormatAdvanced(bool advanced) => advanced ? "Yes" : "No";

    private static string TruncateValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        const int maxLength = 100;
        return value.Length > maxLength ? value[..maxLength] + "..." : value;
    }
}
