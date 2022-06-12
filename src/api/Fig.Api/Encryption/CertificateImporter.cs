using System.Security.Cryptography.X509Certificates;
using Fig.Api.DataImport;
using Fig.Api.Services;

namespace Fig.Api.Encryption;

public class CertificateImporter : ICertificateImporter
{
    private const string PfxFileFilter = "*.pfx";
    private readonly IFileImporter _fileImporter;
    private readonly ILogger<CertificateImporter> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CertificateImporter(ILogger<CertificateImporter> logger, IFileImporter fileImporter,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _fileImporter = fileImporter;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Initialize()
    {
        var path = GetImportFolderPath();
        await _fileImporter.Initialize(path, PfxFileFilter, ImportFile, () => true);
    }

    public void Dispose()
    {
        _fileImporter.Dispose();
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
            _logger.LogInformation($"Importing certificate at path: {path}");

            var certBytes = await File.ReadAllBytesAsync(path);
            var cert = new X509Certificate2(certBytes, "fig", X509KeyStorageFlags.Exportable);

            using var scope = _serviceScopeFactory.CreateScope();
            var encryptionService = scope.ServiceProvider.GetService<IEncryptionService>();
            if (encryptionService is null)
                throw new InvalidOperationException("Unable to find EncryptionService");

            encryptionService.ImportCertificate(cert);
            _logger.LogInformation($"Import of file {path} completed successfully.");
        }
        catch (Exception exception)
        {
            _logger.LogError($"Invalid certificate file: {path}. {exception}");
        }
    }
}