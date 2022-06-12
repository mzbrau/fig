using Fig.Api.DataImport;

namespace Fig.Api.Utils;

public class BackgroundWorker : IBackgroundWorker
{
    private readonly IConfigFileImporter _configFileImporter;

    public BackgroundWorker(IConfigFileImporter configFileImporter)
    {
        _configFileImporter = configFileImporter;
    }


    public async Task Initialize()
    {
        await _configFileImporter.Initialize();
    }
}