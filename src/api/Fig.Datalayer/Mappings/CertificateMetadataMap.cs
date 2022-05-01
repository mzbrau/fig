using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class CertificateMetadataMap : ClassMapping<CertificateMetadataBusinessEntity>
{
    public CertificateMetadataMap()
    {
        Table("certificate_metadata");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Thumbprint, x => x.Column("thumbprint"));
        Property(x => x.ValidFrom, x =>
        {
            x.Column("valid_from");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.ValidTo, x =>
        {
            x.Column("valid_to");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.InUse, x => x.Column("in_use"));
    }
}