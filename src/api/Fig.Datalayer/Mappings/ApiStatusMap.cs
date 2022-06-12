using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class ApiStatusMap : ClassMapping<ApiStatusBusinessEntity>
{
    public ApiStatusMap()
    {
        Table("api_status");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.RuntimeId, x => x.Column("runtime_id"));
        Property(x => x.UptimeSeconds, x => x.Column("uptime_seconds"));
        Property(x => x.LastSeen, x =>
        {
            x.Column("valid_from");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.IpAddress, x => x.Column("ip_address"));
        Property(x => x.Hostname, x => x.Column("hostname"));
        Property(x => x.Version, x => x.Column("version"));
        Property(x => x.IsActive, x => x.Column("is_active"));
        Property(x => x.CertificatesInStoreJson, x => x.Column("certificates_in_store_json"));
    }
}