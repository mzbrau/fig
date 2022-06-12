using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Encryption;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public class EncryptionService : IEncryptionService
{
    private readonly ICertificateStore _certificateStore;
    private readonly ICertificateMetadataRepository _certificateMetadataRepository;
    private readonly ICertificateFactory _certificateFactory;
    private CertificateMetadataBusinessEntity? _certificateMetadata;

    public EncryptionService(ICertificateStore certificateStore,
        ICertificateMetadataRepository certificateMetadataRepository,
        
        ICertificateFactory certificateFactory)
    {
        _certificateStore = certificateStore;
        _certificateMetadataRepository = certificateMetadataRepository;
        _certificateFactory = certificateFactory;
    }

    public int InputLimit => 500; // 500 based on 4096 key length

    public EncryptionResultModel Encrypt(string plainText)
    {
        _certificateMetadata ??= _certificateMetadataRepository.GetInUse();

        X509Certificate2? certificate;
        if (_certificateMetadata == null)
        {
            certificate = CreateNewCertificate();
            _certificateStore.SaveCertificate(certificate);
        }
        else
        {
            certificate = _certificateStore.GetCertificate(_certificateMetadata.Thumbprint);
        }

        if (certificate == null)
            throw new ApplicationException("No certificate found for encryption");

        var encryptedValue = Encrypt(plainText, CreateRsaPublicKey(certificate));
        return new EncryptionResultModel()
        {
            EncryptedValue = encryptedValue,
            Thumbprint = certificate.Thumbprint
        };
    }

    public string Decrypt(string encryptedText, string thumbprint)
    {
        _certificateMetadata ??= _certificateMetadataRepository.GetCertificate(thumbprint);
        
        if (_certificateMetadata == null)
            throw new ApplicationException("No certificate found for decryption");

        var certificate = _certificateStore.GetCertificate(_certificateMetadata.Thumbprint);

        if (certificate == null)
            throw new ApplicationException("No certificate found for decryption");
        
        return Decrypt(encryptedText, CreateRsaPrivateKey(certificate));
    }

    public void ImportCertificate(X509Certificate2 certificate)
    {
        _certificateStore.SaveCertificate(certificate);
        _certificateMetadataRepository.AddCertificate(new CertificateMetadataBusinessEntity()
        {
            Thumbprint = certificate.Thumbprint,
            ValidFrom = certificate.NotBefore.ToUniversalTime(),
            ValidTo = certificate.NotAfter.ToUniversalTime()
        });
    }
    
    private X509Certificate2 CreateNewCertificate()
    {
        /*Key size: 1024. Took 322ms to create
        Max input size: 118. (118bytes) Average encryption time 0.025ms. Max time: 3

        Key size: 2048. Took 418ms to create
        Max input size: 246. (246bytes) Average encryption time 0.008ms. Max time: 1

        Key size: 3072. Took 1872ms to create
        Max input size: 374. (374bytes) Average encryption time 0.061ms. Max time: 12

        Key size: 4096. Took 13312ms to create
        Max input size: 502. (502bytes) Average encryption time 0.033ms. Max time: 2

        Key size: 5120. Took 5796ms to create
        Max input size: 630. (630bytes) Average encryption time 0.142ms. Max time: 1

        Key size: 6144. Took 9596ms to create
        Max input size: 758. (758bytes) Average encryption time 0.267ms. Max time: 2

        Key size: 7168. Took 36039ms to create
        Max input size: 886. (886bytes) Average encryption time 1.019ms. Max time: 3

        Key size: 8192. Took 7994ms to create
        Max input size: 1014. (1014bytes) Average encryption time 1.021ms. Max time: 2

        */
        // Selecting 4096 has this strikes the right balance between creation time and character length.
        // Trying to create key size greater than 8192 results in an exception.
        var cert = _certificateFactory.Create(4096);
        _certificateMetadataRepository.ReplaceInUse(new CertificateMetadataBusinessEntity()
        {
            Thumbprint = cert.Thumbprint,
            ValidFrom = cert.NotBefore.ToUniversalTime(),
            ValidTo = cert.NotAfter.ToUniversalTime()
        });

        return cert;
    }
    
    private string Encrypt(string text, RSA rsa)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        byte[] cipherText = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        return Convert.ToBase64String(cipherText);
    }
    
    private string Decrypt(string text, RSA rsa)
    {
        byte[] data = Convert.FromBase64String(text); 
        byte[] cipherText = rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(cipherText);
    }
    
    private static RSA CreateRsaPublicKey(X509Certificate2 certificate)
    {
        var publicKey = certificate.GetRSAPublicKey();

        if (publicKey == null)
            throw new ApplicationException("Certificate does not have a public key");

        return publicKey;
    }

    private static RSA CreateRsaPrivateKey(X509Certificate2 certificate)
    {
        var privateKey = certificate.GetRSAPrivateKey();
        
        if (privateKey == null)
            throw new ApplicationException("Certificate does not have a private key");

        return privateKey;
    }
}