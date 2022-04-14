using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class ClientStatusMap : ClassMapping<ClientStatusBusinessEntity>
{
    public ClientStatusMap()
    {
        Table("setting_client");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.Instance, x => x.Column("instance"));
        Property(x => x.ClientSecret, x => x.Column("client_secret"));
        Property(x => x.LastRegistration, x => x.Column("last_registration"));
        Property(x => x.LastSettingValueUpdate, x => x.Column("last_update"));
        Property(x => x.LastSeen, x => x.Column("last_seen"));
        Property(x => x.LiveReload, x => x.Column("live_reload"));
        Property(x => x.PollIntervalSeconds, x => x.Column("poll_interval_sec"));
        Property(x => x.IpAddress, x => x.Column("ip_address"));
        Property(x => x.Hostname, x => x.Column("hostname"));
    }
}