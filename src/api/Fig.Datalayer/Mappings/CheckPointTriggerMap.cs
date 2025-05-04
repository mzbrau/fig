using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class CheckPointTriggerMap : ClassMapping<CheckPointTriggerBusinessEntity>
{
    public CheckPointTriggerMap()
    {
        Table(Mapping.CheckPointTriggerTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.AfterEvent, x => x.Column("after_event"));
        Property(x => x.User, x => x.Column("user_name"));
        Property(x => x.HandlingInstance, x => x.Column("handling_instance"));
        Property(x => x.Timestamp, x =>
        {
            x.Column("timestamp");
            x.Type(NHibernateUtil.UtcTicks);
        });
    }
}