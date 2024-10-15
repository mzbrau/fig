using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class CheckPointMap : ClassMapping<CheckPointBusinessEntity>
{
    public CheckPointMap()
    {
        Table(Mapping.CheckPointTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.DataId, x => x.Column("data_id"));
        Property(x => x.Timestamp, x =>
        {
            x.Column("timestamp");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.NumberOfSettings, x => x.Column("no_settings"));
        Property(x => x.NumberOfClients, x => x.Column("no_clients"));
        Property(x => x.AfterEvent, x => x.Column("after_event"));
        Property(x => x.Note, x => x.Column("note"));
        Property(x => x.User, x => x.Column("user"));
    }
}