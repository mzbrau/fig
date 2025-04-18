using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class DeferredChangeMap : ClassMapping<DeferredChangeBusinessEntity>
{
    public DeferredChangeMap()
    {
        Table(Mapping.DeferredChangeTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.RequestingUser, x => x.Column("requesting_user"));
        Property(x => x.ClientName, x => x.Column("client_name"));
        Property(x => x.Instance, x => x.Column("client_instance"));
        Property(x => x.ChangeSetAsJson, x =>
        {
            x.Column("change_set");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.HandlingInstance, x => x.Column("client_instance"));
        Property(x => x.ExecuteAtUtc, x =>
        {
            x.Column("execute_at");
            x.Type(NHibernateUtil.UtcTicks);
        });
    }
}