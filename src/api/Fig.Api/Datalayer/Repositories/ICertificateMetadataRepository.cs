using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICertificateMetadataRepository
{
    IList<CertificateMetadataBusinessEntity> GetAllNonExpiredCertificates();

    CertificateMetadataBusinessEntity? GetInUse();

    CertificateMetadataBusinessEntity? GetCertificate(string thumbprint);

    void ReplaceInUse(CertificateMetadataBusinessEntity certificateMetadata);

    void AddCertificate(CertificateMetadataBusinessEntity certificateMetadata);
}