using Fig.Contracts.ImportExport;
using Fig.Web.Models.ImportExport;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class DataFacade : IDataFacade
{
    private readonly IHttpService _httpService;

    public DataFacade(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public List<DeferredImportClientModel> DeferredClients { get; private set; }

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

    public async Task<FigDataExportDataContract?> ExportSettings(bool decryptSecrets)
    {
        try
        {
            return await _httpService.Get<FigDataExportDataContract>($"data?decryptSecrets={decryptSecrets}");
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    public async Task<FigValueOnlyDataExportDataContract?> ExportValueOnlySettings()
    {
        try
        {
            return await _httpService.Get<FigValueOnlyDataExportDataContract>($"valueonlydata");
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
            return result.Select(a => new DeferredImportClientModel()
            {
                Name = a.Name,
                Instance = a.Instance,
                SettingCount = a.SettingCount,
                RequestingUser = a.ImportingUser
            }).ToList();
        }
        catch (Exception)
        {
            return Array.Empty<DeferredImportClientModel>().ToList();
        }
    }
}