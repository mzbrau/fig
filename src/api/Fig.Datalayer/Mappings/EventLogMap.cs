using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class EventLogMap : ClassMapping<EventLogBusinessEntity>
{
    public EventLogMap()
    {
        Table(Mapping.EventLogsTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Timestamp, x =>
        {
            x.Column("timestamp");
            x.Type(NHibernateUtil.UtcTicks);
            x.Index("event_log_timestamp_index");
        });
        Property(x => x.ClientId, x => x.Column("client_id"));
        Property(x => x.ClientName, x => x.Column("client_name"));
        Property(x => x.SettingName, x => x.Column("setting_name"));
        Property(x => x.EventType, x => x.Column("event_type"));
        Property(x => x.OriginalValueEncrypted, x =>
        {
            x.Column("original_value");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.NewValueEncrypted, x =>
        {
            x.Column("new_value");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.AuthenticatedUser, x => x.Column("authenticated_user"));
        Property(x => x.Message, x =>
        {
            x.Column("message");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.VerificationName, x => x.Column("verification_name"));
        Property(x => x.IpAddress, x => x.Column("ip_address"));
        Property(x => x.Hostname, x => x.Column("hostname"));
        Property(x => x.Instance, x => x.Column("client_instance"));
        Property(x => x.LastEncrypted, x =>
        {
            x.Column("last_encrypted");
            x.Type(NHibernateUtil.UtcTicks);
        });
    }
}