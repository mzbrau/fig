using Fig.Common.Events;
using Fig.Contracts.ImportExport;
using Fig.Web.Events;
using Fig.Web.Models.ImportExport;
using Fig.Web.Services;
using Newtonsoft.Json;
using System.Linq;

namespace Fig.Web.Facades;

public class DataFacade : IDataFacade
{
    private readonly IHttpService _httpService;

    public DataFacade(IHttpService httpService, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            DeferredClients.Clear();
        });
    }

    public List<DeferredImportClientModel> DeferredClients { get; private set; } = new();

    public async Task<ImportResultDataContract?> ImportSettings(FigDataExportDataContract data)
    {
        try
        {
            return await _httpService.Put<ImportResultDataContract>("data", data);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<ImportResultDataContract?> ImportValueOnlySettings(FigValueOnlyDataExportDataContract data)
    {
        try
        {
            return await _httpService.Put<ImportResultDataContract>("valueonlydata", data);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<FigDataExportDataContract?> ExportSettings()
    {
        try
        {
            return await _httpService.GetLarge<FigDataExportDataContract>($"data");
        }
        catch (Exception)
        {
            return null;
        }
    }    public async Task<FigValueOnlyDataExportDataContract?> ExportValueOnlySettings(bool excludeEnvironmentSpecific = false)
    {
        try
        {
            var queryString = excludeEnvironmentSpecific ? "?excludeEnvironmentSpecific=true" : "";
            return await _httpService.GetLarge<FigValueOnlyDataExportDataContract>($"valueonlydata{queryString}");
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<FigValueOnlyDataExportDataContract?> ExportChangeSetSettings(FigValueOnlyDataExportDataContract referenceData, bool excludeEnvironmentSpecific = false)
    {
        try
        {
            // Get current settings
            var currentData = await ExportValueOnlySettings(excludeEnvironmentSpecific);
            if (currentData == null)
                return null;

            // Compare and create change set
            var changeSetClients = new List<SettingClientValueExportDataContract>();
            
            foreach (var currentClient in currentData.Clients)
            {
                var referenceClient = referenceData.Clients.FirstOrDefault(c => c.Name == currentClient.Name && c.Instance == currentClient.Instance);
                var changedSettings = new List<SettingValueExportDataContract>();
                
                foreach (var currentSetting in currentClient.Settings)
                {
                    var referenceSetting = referenceClient?.Settings.FirstOrDefault(s => s.Name == currentSetting.Name);
                    
                    // Include setting if:
                    // 1. It doesn't exist in reference data
                    // 2. The value is different from reference data
                    if (referenceSetting == null || !AreValuesEqual(currentSetting.Value, referenceSetting.Value))
                    {
                        changedSettings.Add(currentSetting);
                    }
                }
                
                if (changedSettings.Any())
                {
                    changeSetClients.Add(new SettingClientValueExportDataContract(
                        currentClient.Name, 
                        currentClient.Instance, 
                        changedSettings));
                }
            }
            
            // Also include any clients that exist in current but not in reference
            foreach (var currentClient in currentData.Clients)
            {
                var referenceClient = referenceData.Clients.FirstOrDefault(c => c.Name == currentClient.Name && c.Instance == currentClient.Instance);
                if (referenceClient == null && !changeSetClients.Any(c => c.Name == currentClient.Name && c.Instance == currentClient.Instance))
                {
                    changeSetClients.Add(currentClient);
                }
            }

            return new FigValueOnlyDataExportDataContract(
                DateTime.UtcNow,
                ImportType.UpdateValues,
                currentData.Version,
                currentData.IsExternallyManaged,
                changeSetClients)
            {
                ExportingServer = currentData.ExportingServer,
                Environment = currentData.Environment
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        
        // Use JSON serialization for deep comparison
        var json1 = JsonConvert.SerializeObject(value1);
        var json2 = JsonConvert.SerializeObject(value2);
        return json1 == json2;
    }

    public async Task RefreshDeferredClients()
    {
        DeferredClients = await GetDeferredImportClients();
    }

    private async Task<List<DeferredImportClientModel>> GetDeferredImportClients()
    {
        try
        {
            var result = (await _httpService.Get<List<DeferredImportClientDataContract>>($"deferredimport"))!;
            return result.Select(a => new DeferredImportClientModel(name: a.Name, instance: a.Instance,
                settingCount: a.SettingCount, requestingUser: a.ImportingUser)).ToList();
        }
        catch (Exception)
        {
            return Array.Empty<DeferredImportClientModel>().ToList();
        }
    }
}