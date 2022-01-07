using Fig.Api.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Api.Datalayer.Mappings;

public class EventLogMap : ClassMapping<EventLogBusinessEntity>
{
    public EventLogMap()
    {
        Table("event_log");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Timestamp, x => x.Column("timestamp"));
        Property(x => x.ClientId, x => x.Column("client_id"));
        Property(x => x.ClientName, x => x.Column("client_name"));
        Property(x => x.SettingName, x => x.Column("setting_name"));
        Property(x => x.EventType, x => x.Column("event_type"));
        Property(x => x.OriginalValue, x => x.Column("original_value"));
        Property(x => x.NewValue, x => x.Column("new_value"));
    }
}