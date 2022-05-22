using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Contracts.ImportExport;
using Newtonsoft.Json;

namespace Fig.Api.DataImport;

public class FileImporter : IFileImporter
{
    private readonly ILogger<FileImporter> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IFileWatcherFactory _fileWatcherFactory;
    private readonly IFileMonitor _fileMonitor;
    private IFileWatcher? _fileWatcher;

    public FileImporter(
        ILogger<FileImporter> logger, 
        IServiceScopeFactory serviceScopeFactory, 
        IFileWatcherFactory fileWatcherFactory, 
        IFileMonitor fileMonitor)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _fileWatcherFactory = fileWatcherFactory;
        _fileMonitor = fileMonitor;
    }
    
    public async Task Initialize()
    {
        if (_fileWatcher is not null)
            return;

        var importFolder = GetImportFolderPath();
        if (importFolder is null)
            return;

        await ImportExistingFiles(importFolder);
        
        _logger.LogInformation($"Watching the import folder for configurations. Folder is: {importFolder}");
        _fileWatcher = _fileWatcherFactory.Create(importFolder, "*.json");
        _fileWatcher.FileCreated += OnFileCreated;
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
    }

    private async Task ImportExistingFiles(string importFolder)
    {
        _logger.LogTrace($"Checking import folder {importFolder} for existing files.");
        foreach (var file in Directory.GetFiles(importFolder, "*.json"))
        {
            await ImportFile(file);
        }
    }

    private string? GetImportFolderPath()
    {
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        if (assembly is null)
        {
            _logger.LogWarning("No entry assembly found. File import disabled.");
            return null;
        }

        var directory = new FileInfo(assembly.Location).DirectoryName;

        if (directory is null)
        {
            _logger.LogWarning("No assembly location found. File import disabled.");
            return null;
        }

        return Path.Combine(directory, "ConfigImport");
    }

    private async void OnFileCreated(object? sender, FileSystemEventArgs e)
    {
        // Reasonable delay for the copy to complete.
        await Task.Delay(100);
        var fileUnlocked = await _fileMonitor.WaitUntilUnlocked(e.FullPath, TimeSpan.FromSeconds(10));

        if (fileUnlocked)
            await ImportFile(e.FullPath);
        else
            _logger.LogError($"Unable to import file {e.FullPath} as the file was locked.");
    }

    private async Task ImportFile(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            _logger.LogInformation($"Importing export file at path: {path}");
            var text = await File.ReadAllTextAsync(path);
            var importData = JsonConvert.DeserializeObject<FigDataExportDataContract>(text);

            if (importData is null)
                throw new InvalidDataException("JSON file could not be deserialized");

            using var scope = _serviceScopeFactory.CreateScope();
            var importExportService = scope.ServiceProvider.GetService<IImportExportService>();
            if (importExportService is null)
                throw new InvalidOperationException("Unable to find ImportExport service");

            var result = await importExportService.Import(importData, ImportMode.FileLoad);
            _logger.LogInformation($"Import of file {path} completed successfully. {result}");
        }
        catch (Exception exception)
        {
            _logger.LogError($"Invalid file for fig import: {path}. {exception}");
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
               _logger.LogError($"Unable to delete file at path {path}. {e.Message}");
            }
        }
    }
}