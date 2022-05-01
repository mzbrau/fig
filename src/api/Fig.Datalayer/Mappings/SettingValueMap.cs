using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class SettingValueMap : ClassMapping<SettingValueBusinessEntity>
{
    public SettingValueMap()
    {
        Table("setting_value_history");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ClientId, x => x.Column("client_id"));
        Property(x => x.SettingName, x => x.Column("setting_name"));
        Property(x => x.ValueType, x => x.Column("value_type"));
        Property(x => x.ValueAsJson, x =>
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
        Property(x => x.IsEncrypted, x => x.Column("is_encrypted"));
    }
}