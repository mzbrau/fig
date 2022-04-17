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
        Bag(x => x.RunSessions,
            x =>
            {
                x.Table("run_sessions");
                x.Lazy(CollectionLazy.NoLazy);
                x.Inverse(false);
                x.Cascade(Cascade.All | Cascade.DeleteOrphans);
                x.Key(a => a.Column(b => b.Name("client_reference")));
            },
            x => x.OneToMany(a => { a.Class(typeof(ClientRunSessionBusinessEntity)); }));
    }
}