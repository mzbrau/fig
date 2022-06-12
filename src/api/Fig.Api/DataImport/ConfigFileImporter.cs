using Fig.Api.Services;
using Fig.Contracts.ImportExport;
using Newtonsoft.Json;

namespace Fig.Api.DataImport;

public class ConfigFileImporter : IConfigFileImporter
{
    private const string JsonFilter = "*.json";
    private readonly ILogger<ConfigFileImporter> _logger;
    private readonly IFileImporter _fileImporter;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ConfigFileImporter(ILogger<ConfigFileImporter> logger, IFileImporter fileImporter, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _fileImporter = fileImporter;
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    public async Task Initialize()
    {
        var path = GetImportFolderPath();
        await _fileImporter.Initialize(path, JsonFilter, ImportFile);
    }

    private string GetImportFolderPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var path = Path.Combine(appData, "Fig", "ConfigImport");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
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