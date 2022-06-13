using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fig.Contracts.ImportExport;
using Fig.Integration.Test.Api.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class CertificateManagementTests : IntegrationTestBase
{
    [Test]
    public async Task ShallReturnListOfCertificates()
    {
        // Do a registration just to ensure that a certificate has been generated.
        await RegisterSettings<AllSettingsAndTypes>();

        var certificates = await GetCertificates();
        
        Assert.That(certificates.Count, Is.GreaterThan(0));
        Assert.That(certificates.Count(a => a.InUse), Is.EqualTo(1));
    }

    [Test]
    public async Task ShallExportCertificate()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var certificates = await GetCertificates();

        var inUseCert = certificates.Single(a => a.InUse);

        var cert = await GetCertificate(inUseCert.Thumbprint);

        Assert.That(cert, Is.Not.Null);
        Assert.That(cert.Thumbprint, Is.EqualTo(cert.Thumbprint));
        Assert.That(cert.HasPrivateKey, Is.EqualTo(true));
        Assert.That(cert.GetRSAPrivateKey(), Is.Not.Null);
    }

    [Test]
    public async Task ShallCreateNewCertificate()
    {
        var cert = await GenerateCertificate();

        Assert.That(cert, Is.Not.Null);
        Assert.That(cert.Thumbprint, Is.Not.Null);
        Assert.That(cert.HasPrivateKey, Is.EqualTo(true));
        Assert.That(cert.GetRSAPrivateKey(), Is.Not.Null);
    }

    // TODO: Make this test work.
    //[Test]
    public async Task ShallMigrateToNewCertificate()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SecretSettings>(secret);
        var export = await ExportData(false);

        var certificates = await GetCertificates();
        var inUseCert = certificates.Single(a => a.InUse);
        
        var secretCert = export.Clients.Single().Settings.Single(a => a.Name == nameof(settings.SecretWithDefault))
            .EncryptionCertificateThumbprint;
        Assert.That(secretCert, Is.EqualTo(inUseCert.Thumbprint), "Settings should be encrypted with the in use certificate");
        
        var newCert = await GenerateCertificate();
        
        byte[] certData = newCert.Export(X509ContentType.Pfx, "fig");
        await File.WriteAllBytesAsync(Path.Combine(GetImportFolderPath(), "MyCert.pfx"), certData);

        // Wait for the cert to be imported.
        await Task.Delay(6000);
        
        var export2 = await ExportData(false);
        
        var certificates2 = await GetCertificates();
        Assert.That(certificates2.Count, Is.AtLeast(2));
        var inUseCert2 = certificates.Single(a => a.InUse);
        
        var secretCert2 = export2.Clients.Single().Settings.Single(a => a.Name == nameof(settings.SecretWithDefault))
            .EncryptionCertificateThumbprint;
        Assert.That(secretCert2, Is.EqualTo(inUseCert2.Thumbprint), "Settings should be encrypted with the in use certificate");
    }
    
    private string GetImportFolderPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var path = Path.Combine(appData, "Fig", "ConfigImport");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    private async Task<X509Certificate2> GenerateCertificate()
    {
        using var httpClient = GetHttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.PutAsync($"/data/certificates", null);

        Assert.That(result, Is.Not.Null, "Getting a new certificate should succeed.");
        var content = await result.Content.ReadAsStringAsync();
        var bytes = JsonConvert.DeserializeObject<byte[]>(content);
        var certPath = Path.Combine(Path.GetTempPath(), "cert.ptx");
        if (File.Exists(certPath))
            File.Delete(certPath);
        
        await File.WriteAllBytesAsync(certPath, bytes);
        return new X509Certificate2(certPath, "fig");
    }

    private async Task<X509Certificate2> GetCertificate(string thumbprint)
    {
        using var httpClient = GetHttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetStringAsync($"/data/certificates/{thumbprint}");

        Assert.That(result, Is.Not.Null, "Getting a certificate should succeed.");
        var bytes = JsonConvert.DeserializeObject<byte[]>(result);
        var certPath = Path.Combine(Path.GetTempPath(), "cert.ptx");
        if (File.Exists(certPath))
            File.Delete(certPath);
        
        await File.WriteAllBytesAsync(certPath, bytes);
        return new X509Certificate2(certPath, "fig");
    }

    private async Task<List<CertificateMetadataDataContract>> GetCertificates()
    {
        using var httpClient = GetHttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetStringAsync($"/data/certificates");

        Assert.That(result, Is.Not.Null, "Getting certificates should succeed.");

        return JsonConvert.DeserializeObject<List<CertificateMetadataDataContract>>(result);
    }
}