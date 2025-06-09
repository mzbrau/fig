using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class ApiStatusMap : ClassMapping<ApiStatusBusinessEntity>
{
    public ApiStatusMap()
    {
        Table(Mapping.ApiStatusTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.RuntimeId, x => x.Column("runtime_id"));
        Property(x => x.StartTimeUtc, x =>
        {
            x.Column("start_time");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.LastSeen, x =>
        {
            x.Column("last_seen");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.IpAddress, x => x.Column("ip_address"));
        Property(x => x.Hostname, x => x.Column("hostname"));
        Property(x => x.Version, x => x.Column("version"));
        Property(x => x.MemoryUsageBytes, x => x.Column("memory_bytes"));
        Property(x => x.RunningUser, x => x.Column("running_user"));
        Property(x => x.TotalRequests, x => x.Column("total_requests"));
        Property(x => x.RequestsPerMinute, x => x.Column("requests_per_minute"));
        Property(x => x.IsActive, x => x.Column("is_active"));
        Property(x => x.SecretHash, x => x.Column("secret_hash"));
        Property(x => x.ConfigurationErrorDetected, x => x.Column("config_error_detected"));
    }
}