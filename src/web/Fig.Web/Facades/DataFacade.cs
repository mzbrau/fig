using Fig.Contracts.ImportExport;
using Fig.Web.Events;
using Fig.Web.Models.ImportExport;
using Fig.Web.Services;

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

    public async Task<FigDataExportDataContract?> ExportSettings(bool excludeSecrets)
    {
        try
        {
            return await _httpService.Get<FigDataExportDataContract>($"data?excludeSecrets={excludeSecrets}");
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    public async Task<FigValueOnlyDataExportDataContract?> ExportValueOnlySettings(bool excludeSecrets)
    {
        try
        {
            return await _httpService.Get<FigValueOnlyDataExportDataContract>($"valueonlydata?excludeSecrets={excludeSecrets}");
        }
        catch (Exception)
        {
            return null;
        }
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