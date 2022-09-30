using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class DeferredClientImportMap : ClassMapping<DeferredClientImportBusinessEntity>
{
    public DeferredClientImportMap()
    {
        Table("deferred_client_import");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.Instance, x => x.Column("instance"));
        Property(x => x.SettingValuesAsJson, x => x.Column("values_as_json"));
    }
}