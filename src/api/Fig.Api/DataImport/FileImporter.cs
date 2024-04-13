using Fig.Api.Utils;

namespace Fig.Api.DataImport;

public class FileImporter : IFileImporter
{
    private readonly IFileMonitor _fileMonitor;
    private readonly IFileWatcherFactory _fileWatcherFactory;
    private readonly ILogger<FileImporter> _logger;
    private readonly SemaphoreSlim _semaphoreSlim = new(1,1);
    private Func<bool> _canImport = null!;
    private IFileWatcher? _fileWatcher;
    private string _filter = ".*";
    private Func<string, Task> _performImport = null!;

    public FileImporter(
        ILogger<FileImporter> logger,
        IFileWatcherFactory fileWatcherFactory,
        IFileMonitor fileMonitor)
    {
        _logger = logger;
        _fileWatcherFactory = fileWatcherFactory;
        _fileMonitor = fileMonitor;
    }

    public async Task Initialize(string importFolder, string filter, Func<string, Task> performImport,
        Func<bool> canImport, CancellationToken stoppingToken)
    {
        if (_fileWatcher is not null)
            return;

        if (importFolder is null)
            return;

        _filter = filter;

        _performImport = performImport;
        _canImport = canImport;
        await ImportExistingFiles(importFolder);

        _logger.LogInformation("Watching the import folder for configurations. Folder is: {ImportFolder}", importFolder);
        _fileWatcher = _fileWatcherFactory.Create(importFolder, _filter);
        stoppingToken.Register(() => _fileWatcher.FileCreated -= OnFileCreated);
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
    }

    private async Task ImportExistingFiles(string importFolder)
    {
        if (!_canImport())
        {
            _logger.LogInformation("File imports are disabled");
            return;
        }

        _logger.LogTrace("Checking import folder {ImportFolder} for existing files", importFolder);
        foreach (var file in Directory.GetFiles(importFolder, _filter))
            await _performImport(file);
    }

    private async void OnFileCreated(object? sender, FileSystemEventArgs args)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (!_canImport())
                return;

            // Reasonable delay for the copy to complete.
            await Task.Delay(200);
            var fileUnlocked = await _fileMonitor.WaitUntilUnlocked(args.FullPath, TimeSpan.FromSeconds(10));

            if (fileUnlocked)
                await _performImport(args.FullPath);
            else
                _logger.LogError("Unable to import file {FullPath} as the file was locked", args.FullPath);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}