using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class ClientRegistrationHistoryMap : ClassMapping<ClientRegistrationHistoryBusinessEntity>
{
    public ClientRegistrationHistoryMap()
    {
        Table(Mapping.ClientRegistrationHistoryTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.RegistrationDateUtc, x =>
        {
            x.Column("registration_date_utc");
            x.Type(NHibernateUtil.UtcTicks);
            x.Index("client_registration_history_date_index");
        });
        Property(x => x.ClientName, x =>
        {
            x.Column("client_name");
            x.Index("client_registration_history_client_name_index");
        });
        Property(x => x.ClientVersion, x => x.Column("client_version"));
        Property(x => x.SettingsJson, x =>
        {
            x.Column("settings_json");
            x.Type(NHibernateUtil.StringClob);
        });
    }
}
