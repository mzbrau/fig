using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class CertificateMetadataConverter : ICertificateMetadataConverter
{
    public CertificateMetadataDataContract Convert(CertificateMetadataBusinessEntity certificateMetadata)
    {
        return new CertificateMetadataDataContract
        {
            Thumbprint = certificateMetadata.Thumbprint,
            ValidFrom = certificateMetadata.ValidFrom,
            ValidTo = certificateMetadata.ValidTo,
            InUse = certificateMetadata.InUse
        };
    }
}