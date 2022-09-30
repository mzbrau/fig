using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Contracts.ImportExport;
using Newtonsoft.Json;

namespace Fig.Api.DataImport;

public class ConfigFileImporter : BackgroundService
{
    private const string JsonFilter = "*.json";
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IFileImporter _fileImporter;
    private readonly ILogger<ConfigFileImporter> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ConfigFileImporter(ILogger<ConfigFileImporter> logger,
        IFileImporter fileImporter,
        IServiceScopeFactory serviceScopeFactory,
        IConfigurationRepository configurationRepository)
    {
        _logger = logger;
        _fileImporter = fileImporter;
        _serviceScopeFactory = serviceScopeFactory;
        _configurationRepository = configurationRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var path = GetImportFolderPath();
        await _fileImporter.Initialize(path, JsonFilter, ImportFile, CanImport);
    }

    private string GetImportFolderPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var path = Path.Combine(appData, "Fig", "ConfigImport");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    private bool CanImport()
    {
        var configuration = _configurationRepository.GetConfiguration();
        return configuration.AllowFileImports;
    }

    private async Task ImportFile(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            _logger.LogInformation($"Importing export file at path: {path}");
            var text = await File.ReadAllTextAsync(path);

            if (text.TryParseJson(out FigDataExportDataContract fullImportData))
            {
                await Import(fullImportData, path);
            }
            else if (text.TryParseJson(out FigValueOnlyDataExportDataContract valueOnlyImportData))
            {
                ImportValueOnly(valueOnlyImportData, path);
            }
            else
            {
                throw new InvalidDataException("JSON file could not be deserialized");
            }
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

    private async Task Import(FigDataExportDataContract importData, string path)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var importExportService = scope.ServiceProvider.GetService<IImportExportService>();
        if (importExportService is null)
            throw new InvalidOperationException("Unable to find ImportExport service");
        
        var result = await importExportService.Import(importData, ImportMode.FileLoad);
        _logger.LogInformation($"Import of full settings file {path} completed successfully. {result}");
    }

    private void ImportValueOnly(FigValueOnlyDataExportDataContract importData, string path)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var importExportService = scope.ServiceProvider.GetService<IImportExportService>();
        if (importExportService is null)
            throw new InvalidOperationException("Unable to find ImportExport service");
        
        var result = importExportService.ValueOnlyImport(importData, ImportMode.FileLoad);
        _logger.LogInformation($"Import of value only file {path} completed successfully. {result}");
    }
}