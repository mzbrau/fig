using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class ClientStatusMap : ClassMapping<ClientStatusBusinessEntity>
{
    public ClientStatusMap()
    {
        Table(Mapping.SettingClientsTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.Description, x =>
        {
            x.Column("description");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.Instance, x => x.Column("client_instance"));
        Property(x => x.ClientSecret, x => x.Column("client_secret"));
        Property(x => x.PreviousClientSecret, x => x.Column("previous_client_secret"));
        Property(x => x.PreviousClientSecretExpiryUtc, x =>
        {
            x.Column("previous_client_secret_expiry");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.LastRegistration, x =>
        {
            x.Column("last_registration");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.LastSettingValueUpdate, x =>
        {
            x.Column("last_update");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Bag(x => x.RunSessions,
            x =>
            {
                x.Table(Mapping.RunSessionsTable);
                x.Lazy(CollectionLazy.NoLazy);
                x.Inverse(false);
                x.Cascade(Cascade.All | Cascade.DeleteOrphans);
                x.Key(a => a.Column(b => b.Name("client_reference")));
            },
            x => x.OneToMany(a => { a.Class(typeof(ClientRunSessionBusinessEntity)); }));
    }
}