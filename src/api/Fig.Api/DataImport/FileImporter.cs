using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;

namespace Fig.Api.DataImport;

public class FileImporter : IFileImporter
{
    private readonly ILogger<FileImporter> _logger;
    private readonly IFileWatcherFactory _fileWatcherFactory;
    private readonly IFileMonitor _fileMonitor;
    private readonly IConfigurationRepository _configurationRepository;
    private IFileWatcher? _fileWatcher;
    private Func<string, Task> _performImport = null!;

    public FileImporter(
        ILogger<FileImporter> logger,
        IFileWatcherFactory fileWatcherFactory, 
        IFileMonitor fileMonitor,
        IConfigurationRepository configurationRepository)
    {
        _logger = logger;
        _fileWatcherFactory = fileWatcherFactory;
        _fileMonitor = fileMonitor;
        _configurationRepository = configurationRepository;
    }
    
    public async Task Initialize(string? importFolder, string filter, Func<string, Task> performImport)
    {
        if (_fileWatcher is not null)
            return;

        if (importFolder is null)
            return;

        _performImport = performImport;
        await ImportExistingFiles(importFolder);
        
        _logger.LogInformation($"Watching the import folder for configurations. Folder is: {importFolder}");
        _fileWatcher = _fileWatcherFactory.Create(importFolder, filter);
        _fileWatcher.FileCreated += OnFileCreated;
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
    }

    private async Task ImportExistingFiles(string importFolder)
    {
        var configuration = _configurationRepository.GetConfiguration();
        if (!configuration.AllowFileImports)
        {
            _logger.LogInformation("File imports are disabled");
            return;
        }

        _logger.LogTrace($"Checking import folder {importFolder} for existing files.");
        foreach (var file in Directory.GetFiles(importFolder, "*.json"))
        {
            await _performImport(file);
        }
    }

    private async void OnFileCreated(object? sender, FileSystemEventArgs e)
    {
        var configuration = _configurationRepository.GetConfiguration();
        if (!configuration.AllowFileImports)
            return;
        
        // Reasonable delay for the copy to complete.
        await Task.Delay(100);
        var fileUnlocked = await _fileMonitor.WaitUntilUnlocked(e.FullPath, TimeSpan.FromSeconds(10));

        if (fileUnlocked)
            await _performImport(e.FullPath);
        else
            _logger.LogError($"Unable to import file {e.FullPath} as the file was locked.");
    }

    
}