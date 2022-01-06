using Fig.Api.BusinessEntities;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Api.Datalayer.Mappings;

public class SettingValueMap : ClassMapping<HistoricalSettingValueBusinessEntity>
{
    public SettingValueMap()
    {
        Table("setting_value_history");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ValueType, x => x.Column("value_type"));
        Property(x => x.ValueAsJson, x =>
        {
            x.Column("value_json");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.ChangedAt, x => x.Column("changed_at"));
    }
}