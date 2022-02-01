using CertificateManager;
using Fig.Api.Utils;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Fig.Api.Encryption;

public class CertificateStore : ICertificateStore
{
    private const string FigStore = "figstore";
    private readonly ImportExportCertificate _importExportCertificate;
    private Dictionary<string, X509Certificate2> _certificateCache = new();

    public CertificateStore(ImportExportCertificate importExportCertificate)
    {
        _importExportCertificate = importExportCertificate;
    }

    public X509Certificate2? GetCertificate(string thumbprint)
    {
        if (_certificateCache.ContainsKey(thumbprint))
            return _certificateCache[thumbprint];

        using var store = new X509Store(FigStore, StoreLocation.CurrentUser);
        try
        {
            store.Open(OpenFlags.ReadWrite);
            var cert = store.Certificates.FirstOrDefault(a => a.Thumbprint == thumbprint);

            if (cert != null && !_certificateCache.ContainsKey(cert.Thumbprint))
            {
                _certificateCache.Add(cert.Thumbprint, cert);
            }

            return cert;
        }
        finally
        {
            store.Close();
        }
    }

    public void SaveCertificate(X509Certificate2 certificate)
    {
        if (!_certificateCache.ContainsKey(certificate.Thumbprint))
            _certificateCache.Add(certificate.Thumbprint, certificate);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            certificate = MakeCertificatePrivateKeyExportable(certificate);
        }

        using var store = new X509Store(FigStore, StoreLocation.CurrentUser);
        try
        {
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
        }
        finally
        {
            store.Close();
        }
    }

    public X509Certificate2 MakeCertificatePrivateKeyExportable(X509Certificate2 certificate)
    {
        var password = Guid.NewGuid().ToString();
        var bytes = _importExportCertificate.ExportSelfSignedCertificatePfx(password, certificate);

        using var file = new TempFile(bytes, "fig_cert.pfx");
        X509Certificate2 cert = new X509Certificate2(file.FilePath, password,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        return cert;
    }
}