using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class CommonEnumerationMap : ClassMapping<CommonEnumerationBusinessEntity>
{
    public CommonEnumerationMap()
    {
        Table("common_enumerations");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.EnumerationAsJson, x => x.Column("enumeration"));
    }
}