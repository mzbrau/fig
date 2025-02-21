using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class SettingChangeMap : ClassMapping<SettingChangeBusinessEntity>
{
    public SettingChangeMap()
    {
        Table(Mapping.SettingChangeTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ServerName, x => x.Column("server_name"));
        Property(x => x.LastChange, x =>
        {
            x.Column("timestamp");
            x.Type(NHibernateUtil.UtcTicks);
        });
    }
}