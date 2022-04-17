using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using NHibernate.Type;

namespace Fig.Datalayer.Mappings;

public class ClientRunSessionMap : ClassMapping<ClientRunSessionBusinessEntity>
{
    public ClientRunSessionMap()
    {
        Table("run_sessions");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.RunSessionId, x => x.Column("run_session_id"));
        Property(x => x.LastSeen, x => x.Column("last_seen"));
        Property(x => x.LiveReload, x => x.Column("live_reload"));
        Property(x => x.PollIntervalMs, x => x.Column("poll_interval_ms"));
        Property(x => x.UptimeSeconds, x => x.Column("up_time_sec"));
        Property(x => x.IpAddress, x => x.Column("ip_address"));
        Property(x => x.Hostname, x => x.Column("hostname"));
    }
}