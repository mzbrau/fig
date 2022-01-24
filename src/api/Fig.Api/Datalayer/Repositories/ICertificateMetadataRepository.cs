using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICertificateMetadataRepository
{
    CertificateMetadataBusinessEntity? GetInUse();

    void ReplaceInUse(CertificateMetadataBusinessEntity certificateMetadata);
}