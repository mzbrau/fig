using Fig.Api.ApiStatus;
using Fig.Api.DataImport;

namespace Fig.Api.Utils;

public class BackgroundWorker : IBackgroundWorker
{
    private readonly IApiStatusMonitor _apiStatusMonitor;
    private readonly IConfigFileImporter _configFileImporter;

    public BackgroundWorker(IConfigFileImporter configFileImporter,
        IApiStatusMonitor apiStatusMonitor)
    {
        _configFileImporter = configFileImporter;
        _apiStatusMonitor = apiStatusMonitor;
    }

    public async Task Initialize()
    {
        await _configFileImporter.Initialize();
        _apiStatusMonitor.Start();
    }
}