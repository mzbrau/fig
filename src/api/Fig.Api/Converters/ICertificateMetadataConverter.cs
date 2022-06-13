using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ICertificateMetadataConverter
{
    CertificateMetadataDataContract Convert(CertificateMetadataBusinessEntity certificateMetadata);
}