using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class DeferredClientImportMap : ClassMapping<DeferredClientImportBusinessEntity>
{
    public DeferredClientImportMap()
    {
        Table(Mapping.DeferredClientImportsTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.Instance, x => x.Column("client_instance"));
        Property(x => x.SettingValuesAsJson, x =>
        {
            x.Column("values_as_json");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.SettingCount, x => x.Column("setting_count"));
        Property(x => x.AuthenticatedUser, x => x.Column("authenticated_user"));
        Property(x => x.ImportTime, x =>
        {
            x.Column("timestamp");
            x.Type(NHibernateUtil.UtcTicks);
            x.Index("import_time");
        });
    }
}