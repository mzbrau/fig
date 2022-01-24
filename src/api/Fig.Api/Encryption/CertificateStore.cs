using System.Security.Cryptography.X509Certificates;

namespace Fig.Api.Encryption;

public class CertificateStore : ICertificateStore
{
    private const string FigStore = "figstore";
    private Dictionary<string, X509Certificate2> _certificateCache = new();

    public X509Certificate2? GetCertificate(string thumbprint)
    {
        if (_certificateCache.ContainsKey(thumbprint))
            return _certificateCache[thumbprint];
        
        using var store = new X509Store (FigStore, StoreLocation.CurrentUser);
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

        using var store = new X509Store (FigStore, StoreLocation.CurrentUser);
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
}