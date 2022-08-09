using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class LookupTableMap : ClassMapping<LookupTableBusinessEntity>
{
    public LookupTableMap()
    {
        Table("lookup_tables");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.LookupTableAsJson, x => x.Column("lookup_table"));
    }
}