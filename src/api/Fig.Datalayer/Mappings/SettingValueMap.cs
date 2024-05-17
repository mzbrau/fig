using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class SettingValueMap : ClassMapping<SettingValueBusinessEntity>
{
    public SettingValueMap()
    {
        Table(Mapping.SettingValueHistoryTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ClientId, x => x.Column("client_id"));
        Property(x => x.SettingName, x => x.Column("setting_name"));
        Property(x => x.ValueAsJsonEncrypted, x =>
        {
            x.Column("value_json");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.ChangedAt, x =>
        {
            x.Column("changed_at");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.ChangedBy, x => x.Column("changed_by"));
        Property(x => x.LastEncrypted, x =>
        {
            x.Column("last_encrypted");
            x.Type(NHibernateUtil.UtcTicks);
        });
    }
}