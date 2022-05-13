using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICertificateMetadataRepository
{
    CertificateMetadataBusinessEntity? GetInUse();

    CertificateMetadataBusinessEntity? GetCertificate(string thumbprint);

    void ReplaceInUse(CertificateMetadataBusinessEntity certificateMetadata);
}