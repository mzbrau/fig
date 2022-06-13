using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CertificateManager;
using CertificateManager.Models;

namespace Fig.Api.Encryption;

public class CertificateFactory : ICertificateFactory
{
    private const string CertificateName = "FigEncryptionCertificate";
    private readonly CreateCertificates _createCertificates;

    public CertificateFactory(CreateCertificates createCertificates)
    {
        _createCertificates = createCertificates;
    }

    public X509Certificate2 Create(int keySize)
    {
        var basicConstraints = new BasicConstraints
        {
            CertificateAuthority = true,
            HasPathLengthConstraint = true,
            PathLengthConstraint = 2,
            Critical = false
        };

        var subjectAlternativeName = new SubjectAlternativeName
        {
            DnsName = new List<string>
            {
                CertificateName,
            }
        };

        // TODO: Remove the ones that are not required.
        var x509KeyUsageFlags = X509KeyUsageFlags.KeyCertSign
                                | X509KeyUsageFlags.DigitalSignature
                                | X509KeyUsageFlags.KeyEncipherment
                                | X509KeyUsageFlags.CrlSign
                                | X509KeyUsageFlags.DataEncipherment
                                | X509KeyUsageFlags.NonRepudiation
                                | X509KeyUsageFlags.KeyAgreement;

        // only if mtls is used
        var enhancedKeyUsages = new OidCollection
        {
            OidLookup.CodeSigning,
            OidLookup.SecureEmail,
            OidLookup.TimeStamping
        };

        var certificate = _createCertificates.NewRsaSelfSignedCertificate(
            new DistinguishedName {CommonName = CertificateName},
            basicConstraints,
            new ValidityPeriod
            {
                ValidFrom = DateTimeOffset.UtcNow,
                ValidTo = DateTimeOffset.UtcNow.AddYears(10)
            },
            subjectAlternativeName,
            enhancedKeyUsages,
            x509KeyUsageFlags,
            new RsaConfiguration
            {
                KeySize = keySize,
                RSASignaturePadding = RSASignaturePadding.Pkcs1,
                HashAlgorithmName = HashAlgorithmName.SHA256
            });

        return certificate;
    }
}