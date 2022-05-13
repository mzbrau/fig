using Fig.Contracts.ImportExport;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class DataFacade : IDataFacade
{
    private readonly IHttpService _httpService;

    public DataFacade(IHttpService httpService)
    {
        _httpService = httpService;
    }

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
}