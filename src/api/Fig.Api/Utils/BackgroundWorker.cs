using Fig.Api.ApiStatus;
using Fig.Api.DataImport;
using Fig.Api.Encryption;

namespace Fig.Api.Utils;

public class BackgroundWorker : IBackgroundWorker
{
    private readonly IApiStatusMonitor _apiStatusMonitor;
    private readonly ICertificateImporter _certificateImporter;
    private readonly IConfigFileImporter _configFileImporter;

    public BackgroundWorker(IConfigFileImporter configFileImporter,
        IApiStatusMonitor apiStatusMonitor,
        ICertificateImporter certificateImporter)
    {
        _configFileImporter = configFileImporter;
        _apiStatusMonitor = apiStatusMonitor;
        _certificateImporter = certificateImporter;
    }

    public async Task Initialize()
    {
        await _configFileImporter.Initialize();
        await _certificateImporter.Initialize();
        _apiStatusMonitor.Start();
    }
}