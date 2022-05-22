using Fig.Api.DataImport;

namespace Fig.Api.Utils;

public class BackgroundWorker : IBackgroundWorker
{
    private readonly IFileImporter _fileImporter;

    public BackgroundWorker(IFileImporter fileImporter)
    {
        _fileImporter = fileImporter;
    }


    public async Task Initialize()
    {
        await _fileImporter.Initialize();
    }
}