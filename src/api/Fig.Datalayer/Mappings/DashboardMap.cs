using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class DashboardMap : ClassMapping<DashboardBusinessEntity>
{
    public DashboardMap()
    {
        Table(Mapping.DashboardsTable);
        Lazy(false);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x =>
        {
            x.Column("name");
            x.UniqueKey("ux_dashboards_name");
        });
        Property(x => x.Description, x =>
        {
            x.Column("description");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.AdminOnly, x => x.Column("admin_only"));
        Property(x => x.DefinitionJson, x =>
        {
            x.Column("definition_json");
            x.Type(NHibernateUtil.StringClob);
        });
        Property(x => x.CreatedAt, x =>
        {
            x.Column("created_at");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.LastModifiedAt, x =>
        {
            x.Column("last_modified_at");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.LastModifiedBy, x => x.Column("last_modified_by"));
    }
}
